# DiscForge CHD

[Português (Brasil)](docs/pt-BR/README.md) · [Report a bug](mailto:andrethiesen@live.com)

Windows x64 application for converting PlayStation 1 and PlayStation 2 games to CHD.

## Install

Download the portable ZIP from the [latest release](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest). Extract the entire archive and run `DiscForge-CHD.exe`. Keep every folder from the archive together. .NET and Windows App SDK are included. Windows 10 2004 or newer is required; Windows 11 is recommended. The game cover panel uses the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). Conversion remains available if WebView2 is missing.

- WinUI 3 interface with cards, expandable advanced settings and contextual help.
- WebView2 game panel, serial-based covers, custom icon and build date.
- PS2 CD: `createcd`, default hunk 2448. DVD: `createdvd`, default hunk 2048. Both are configurable.
- SharpCompress extraction with 7-Zip fallback for unsupported formats, methods and split volumes.
- PS1/PS2 classification from disc boot information before conversion; existing outputs are checked after extraction.
- CHD verification and optional deletion after success.
- Per-entry timer, application and encoder CPU/I/O metrics, and automatic thread count.
- Immediate stop, last-used input folder, default optimized output folder and codec checkboxes.
- Version-aware output names, safe multi-track BIN reconstruction and PS1 covers.
- English and Brazilian Portuguese interface; diagnostic report review and email draft.

## Development

C# 14 / .NET 10. The SDK is pinned in `global.json`; package versions are locked in `fontes/packages.lock.json`. Run `fontes/build.ps1` to build a release. See [source structure](fontes/README.md), [tests](TESTS.txt), [publishing](PUBLISHING.md) and [third-party components](THIRD-PARTY.md).

The current branch is **master**. Previous versions remain in tags. Local distributions are stored under `versions/X.Y.Z/`; downloadable binaries are attached to GitHub Releases. Bug reports: [andrethiesen@live.com](mailto:andrethiesen@live.com).
