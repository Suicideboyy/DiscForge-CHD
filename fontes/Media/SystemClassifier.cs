using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Reads the ISO 9660 root directory and SYSTEM.CNF. Disc size and serial alone
// are not proof of a console generation: both PS1 and PS2 can use CDs.
static class SystemClassifier
{
    public static string Detect(string path, string cueText, string root)
        => Detect(path, cueText, root, out _);

    public static string Detect(string path, string cueText, string root, out string serial)
    {
        serial = "";
        string image = path;
        int sector = 2048, offset = 0;
        if (MediaFiles.Extension(path) == ".cue")
        {
            var match = Regex.Match(cueText ?? File.ReadAllText(path),
                "(?im)^\\s*FILE\\s+(?:\"([^\"]+)\"|(\\S+))\\s+\\S+\\s+TRACK\\s+\\d+\\s+(MODE[12]/(?:2352|2048))");
            if (!match.Success)
            {
                return "";
            }

            string relative = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            image = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, relative));
            if (!FileSystemPaths.IsInside(image, root))
            {
                throw new IOException("CUE reference is outside the input directory.");
            }

            FileSystemPaths.EnsureNoLinks(image);
            if (match.Groups[3].Value.EndsWith("2352", StringComparison.Ordinal))
            {
                sector = 2352;
                offset = match.Groups[3].Value.StartsWith("MODE2", StringComparison.OrdinalIgnoreCase)
                    ? 24 : 16;
            }
        }
        else if (MediaFiles.Extension(path) is ".bin" or ".img")
        {
            byte[] header = new byte[16];
            using var probe = File.OpenRead(path);
            if (probe.Length >= 2352 && probe.Length % 2352 == 0)
            {
                probe.ReadExactly(header);
                if (header[0] == 0 && header[11] == 0
                    && header.Skip(1).Take(10).All(b => b == 255))
                {
                    sector = 2352;
                    offset = header[15] == 2 ? 24 : 16;
                }
            }
        }

        using var stream = File.OpenRead(image);
        byte[] pvd = ReadSector(stream, 16, sector, offset);
        if (pvd.Length != 2048 || pvd[0] != 1
            || Encoding.ASCII.GetString(pvd, 1, 5) != "CD001")
        {
            return "";
        }

        int rootSector = BitConverter.ToInt32(pvd, 158);
        int rootLength = BitConverter.ToInt32(pvd, 166);
        if (rootSector < 0 || rootLength < 1 || rootLength > 16 * 1024 * 1024)
        {
            return "";
        }

        byte[] directory = ReadExtent(stream, rootSector, rootLength, sector, offset);
        for (int i = 0; i < directory.Length;)
        {
            int length = directory[i];
            if (length == 0)
            {
                i = (i / 2048 + 1) * 2048;
                continue;
            }

            if (length < 34 || i + length > directory.Length)
            {
                break;
            }

            int nameLength = directory[i + 32];
            if (nameLength <= length - 33)
            {
                string name = Encoding.ASCII.GetString(directory, i + 33, nameLength);
                if (Regex.IsMatch(name, @"(?i)^SYSTEM\.CNF(?:;\d+)?$"))
                {
                    int start = BitConverter.ToInt32(directory, i + 2);
                    int size = BitConverter.ToInt32(directory, i + 10);
                    if (start < 0 || size < 1 || size > 64 * 1024)
                    {
                        return "";
                    }

                    string cnf = Encoding.ASCII.GetString(ReadExtent(stream, start, size, sector, offset));
                    var executable = Regex.Match(cnf,
                        @"(?im)^\s*BOOT2?\s*=\s*cdrom0?:\s*[\\/]*([A-Z]{4})[_-](\d{3})[.\-_](\d{2})");
                    if (executable.Success)
                        serial = executable.Groups[1].Value.ToUpperInvariant() + "-"
                            + executable.Groups[2].Value + executable.Groups[3].Value;
                    bool ps2 = Regex.IsMatch(cnf, @"(?im)^\s*BOOT2\s*=\s*cdrom0?:");
                    bool ps1 = Regex.IsMatch(cnf, @"(?im)^\s*BOOT\s*=\s*cdrom0?:");
                    return ps2 == ps1 ? "" : ps2 ? "PS2" : "PS1";
                }
            }

            i += length;
        }

        return "";
    }

    static byte[] ReadSector(Stream stream, int index, int sector, int offset)
    {
        long position = (long)index * sector + offset;
        if (index < 0 || position < 0 || position + 2048 > stream.Length)
        {
            return Array.Empty<byte>();
        }

        var bytes = new byte[2048];
        stream.Position = position;
        stream.ReadExactly(bytes);
        return bytes;
    }

    static byte[] ReadExtent(Stream stream, int start, int length, int sector, int offset)
    {
        var bytes = new byte[length];
        for (int i = 0; i < length; i += 2048)
        {
            var block = ReadSector(stream, start + i / 2048, sector, offset);
            if (block.Length == 0)
            {
                return Array.Empty<byte>();
            }

            Array.Copy(block, 0, bytes, i, Math.Min(2048, length - i));
        }

        return bytes;
    }
}
