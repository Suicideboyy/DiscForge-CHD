// DiscForge CHD 2.5.0 - genuine CHD v4 compatibility converter.
// SPDX-License-Identifier: BSD-3-Clause
#include "chd.h"
#include "hashing.h"
#define ZLIB_CONST
#include "zlib/zlib.h"
#include <algorithm>
#include <array>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <vector>

namespace {
std::string utf8(const std::filesystem::path &path) {
    auto value = path.u8string();
    return std::string(reinterpret_cast<const char *>(value.data()), value.size());
}
void put(uint8_t *p, uint64_t value, unsigned bytes) {
    for (unsigned i = bytes; i; --i) { p[i - 1] = uint8_t(value); value >>= 8; }
}
void require(std::error_condition error) {
    if (error) throw std::runtime_error(error.message());
}
struct Metadata {
    std::vector<uint8_t> data;
    uint32_t tag;
    uint8_t flags;
};
void write(std::fstream &file, const void *data, size_t size) {
    file.write(static_cast<const char *>(data), size);
    if (!file) throw std::runtime_error("Output write failed.");
}
// CHD v4 uses raw DEFLATE streams, CRC32 per hunk and a fixed 16-byte map.
std::vector<uint8_t> compress(const std::vector<uint8_t> &data) {
    z_stream stream{};
    if (deflateInit2(&stream, 9, Z_DEFLATED, -MAX_WBITS, 8, Z_DEFAULT_STRATEGY) != Z_OK)
        throw std::runtime_error("Cannot initialize legacy zlib codec.");
    std::vector<uint8_t> output(deflateBound(&stream, data.size()));
    stream.next_in = const_cast<Bytef *>(data.data());
    stream.avail_in = data.size();
    stream.next_out = output.data();
    stream.avail_out = output.size();
    int result = deflate(&stream, Z_FINISH);
    size_t size = stream.total_out;
    deflateEnd(&stream);
    if (result != Z_STREAM_END) throw std::runtime_error("Legacy compression failed.");
    output.resize(size);
    return output;
}

void convert(const std::filesystem::path &input, const std::filesystem::path &output) {
    chd_file source;
    require(source.open(utf8(input)));
    std::vector<Metadata> metadata;
    bool cd = false;
    for (uint32_t index = 0;; ++index) {
        Metadata entry;
        auto error = source.read_metadata(CHDMETATAG_WILDCARD, index,
            entry.data, entry.tag, entry.flags);
        if (error == chd_file::error::METADATA_NOT_FOUND) break;
        require(error);
        cd |= entry.tag == CDROM_TRACK_METADATA_TAG || entry.tag == CDROM_TRACK_METADATA2_TAG;
        metadata.push_back(std::move(entry));
    }
    // Four CD frames match modern track padding and legacy hunk-based padding.
    uint32_t hunk = cd ? 9792 : 2048;
    uint64_t logical = source.logical_bytes();
    uint64_t count64 = (logical + hunk - 1) / hunk;
    if (!logical || count64 > UINT32_MAX) throw std::runtime_error("Image exceeds v4 limits.");
    uint32_t count = uint32_t(count64);
    std::fstream file(output, std::ios::binary | std::ios::in | std::ios::out | std::ios::trunc);
    if (!file) throw std::runtime_error("Cannot create output file.");
    std::array<uint8_t, 108> header{};
    std::memcpy(header.data(), "MComprHD", 8);
    put(header.data() + 8, 108, 4);
    put(header.data() + 12, 4, 4);
    put(header.data() + 20, 1, 4);
    put(header.data() + 24, count, 4);
    put(header.data() + 28, logical, 8);
    put(header.data() + 44, hunk, 4);
    write(file, header.data(), header.size());
    std::array<uint8_t, 16> blank{};
    for (uint32_t i = 0; i < count; ++i) write(file, blank.data(), blank.size());
    const char cookie[16] = "EndOfListCookie";
    write(file, cookie, sizeof(cookie));
    std::vector<uint8_t> data(hunk);
    util::sha1_creator raw;
    for (uint32_t i = 0; i < count; ++i) {
        std::fill(data.begin(), data.end(), 0);
        uint32_t bytes = uint32_t(std::min<uint64_t>(hunk, logical - uint64_t(i) * hunk));
        require(source.read_bytes(uint64_t(i) * hunk, data.data(), bytes));
        raw.append(data.data(), bytes);
        auto compressed = compress(data);
        bool useCompressed = compressed.size() < data.size();
        auto &payload = useCompressed ? compressed : data;
        uint64_t offset = uint64_t(file.tellp());
        write(file, payload.data(), payload.size());
        uint64_t end = uint64_t(file.tellp());
        std::array<uint8_t, 16> map{};
        put(map.data(), offset, 8);
        put(map.data() + 8, util::crc32_creator::simple(data.data(), data.size()), 4);
        put(map.data() + 12, payload.size() & 65535, 2);
        map[14] = uint8_t(payload.size() >> 16);
        map[15] = useCompressed ? 1 : 2;
        file.seekp(108 + uint64_t(i) * 16);
        write(file, map.data(), map.size());
        file.seekp(end);
        if (i % 1000 == 0 || i + 1 == count)
            std::cerr << "Converting v4: " << (100.0 * (i + 1) / count) << "%\r";
    }
    auto rawHash = raw.finish();
    if (rawHash != source.raw_sha1()) throw std::runtime_error("Input raw SHA1 mismatch.");
    uint64_t metadataOffset = metadata.empty() ? 0 : uint64_t(file.tellp());
    for (size_t i = 0; i < metadata.size(); ++i) {
        auto &entry = metadata[i];
        if (entry.data.size() > 0xffffff) throw std::runtime_error("Metadata exceeds v4 limits.");
        std::array<uint8_t, 16> node{};
        put(node.data(), entry.tag, 4);
        put(node.data() + 4, entry.data.size(), 4);
        node[4] = entry.flags;
        if (i + 1 < metadata.size())
            put(node.data() + 8, uint64_t(file.tellp()) + 16 + entry.data.size(), 8);
        write(file, node.data(), node.size());
        write(file, entry.data.data(), entry.data.size());
    }
    put(header.data() + 36, metadataOffset, 8);
    auto overall = source.compute_overall_sha1(rawHash);
    if (overall != source.sha1()) throw std::runtime_error("Input metadata SHA1 mismatch.");
    std::memcpy(header.data() + 48, &overall, 20);
    std::memcpy(header.data() + 88, &rawHash, 20);
    file.seekp(0);
    write(file, header.data(), header.size());
    file.close();
    chd_file check;
    require(check.open(utf8(output)));
    if (check.version() != 4 || check.logical_bytes() != logical || check.sha1() != overall)
        throw std::runtime_error("Output header verification failed.");
    // Read every output hunk: decoder checks its CRC independently of the writer.
    util::sha1_creator verified;
    for (uint32_t i = 0; i < count; ++i) {
        require(check.read_hunk(i, data.data()));
        uint32_t bytes = uint32_t(std::min<uint64_t>(hunk, logical - uint64_t(i) * hunk));
        verified.append(data.data(), bytes);
    }
    if (verified.finish() != rawHash) throw std::runtime_error("Output payload SHA1 mismatch.");
    std::cerr << "\nVerified CHD version 4.\n";
}
}
int wmain(int argc, wchar_t **argv) {
    if (argc != 3) {
        std::cerr << "DiscForge CHD 2.5.0 v4 converter\nUsage: chd-v4 input.chd output.chd\n";
        return 2;
    }
    std::filesystem::path output = argv[2];
    if (std::filesystem::exists(output)) { std::cerr << "Output already exists.\n"; return 2; }
    try {
        convert(std::filesystem::path(argv[1]), output);
        return 0;
    } catch (const std::exception &error) {
        std::error_code ignored;
        std::filesystem::remove(output, ignored);
        std::cerr << "ERROR: " << error.what() << '\n';
        return 1;
    } catch (...) {
        std::error_code ignored;
        std::filesystem::remove(output, ignored);
        std::cerr << "ERROR: CHD conversion failed.\n";
        return 1;
    }
}
