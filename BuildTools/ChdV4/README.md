# CHD v4 converter - DiscForge CHD 2.5.0

`chd-v4.exe input.chd output.chd` converts a verified CHD to a genuine
version 4 container. It refuses existing outputs and removes incomplete output
on conversion errors. All output hunks are decoded and checked before success.

The converter uses legacy raw-DEFLATE compression, CRC32 hunk maps, the 108-byte
v4 header, and original metadata. Logical data SHA1 and metadata SHA1 must match
the input. CD hunks contain four 2448-byte frames (9792 bytes), matching modern
track padding and the historical reader's hunk alignment. DVD hunks are 2048
bytes. Modern codec selections do not apply to the v4 output.

CHD v4 does not guarantee that a particular device supports the disc format,
metadata, or firmware requirements. Older readers may lack DVD support.

## Rebuild

Use MAME source commit `a5da980efd1d1143934e322e547b6164edc867e8`
and its release64 utility libraries, built with GCC 16.2.0 for Windows x64.
The supplied binary uses Zen 3 optimization, matching the bundled chdman.

```powershell
powershell -ExecutionPolicy Bypass -File BuildTools/ChdV4/build.ps1 `
  -MameRoot C:/build/mame `
  -Compiler C:/msys64/ucrt64/bin/g++.exe `
  -LibraryRoot C:/build/mame/build-zen3/mingw-gcc/bin/x64/Release
```

Libraries: MAME utilities/Windows OSD, zlib, Zstandard, FLAC, Expat,
utf8proc and the LZMA SDK. LZMA SDK is a library used to read existing CHDs;
this converter does not depend on the 7-Zip application or `7z.dll`.
Preserve MAME and third-party license notices distributed with DiscForge CHD.
Source: https://github.com/mamedev/mame/tree/a5da980efd1d1143934e322e547b6164edc867e8

## Verification

Run bundled chdman's `verify`, then extract CD or DVD and compare SHA256 with
the original extracted image. Inspect header bytes 12-15: `00 00 00 04`.
Test single-track and multitrack CD, DVD, and non-ASCII input/output paths.
