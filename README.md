# DiscForge CHD — PS1 and PS2 CHD Converter for Windows

[Português (Brasil)](docs/pt-BR/README.md) · [Report a bug](mailto:andrethiesen@live.com)

## Download DiscForge CHD for Windows — PS1 and PS2 converter

Convert PlayStation ISO and BIN/CUE images to CHD with a Windows graphical interface. Download the complete portable package; no separate .NET installation is required.

| Download | Contents |
| --- | --- |
| **[Download DiscForge CHD 2.6.1 — Windows x64](https://github.com/Suicideboyy/DiscForge-CHD/releases/download/v2.6.1/DiscForge-CHD-2.6.1-win-x64.zip)** | Complete application, encoder and required runtime files |
| [Download source code — 2.6.1](https://github.com/Suicideboyy/DiscForge-CHD/releases/download/v2.6.1/DiscForge-CHD-2.6.1-source.zip) | Source archive for this stable release |
| [Download SHA-256 checksums](https://github.com/Suicideboyy/DiscForge-CHD/releases/download/v2.6.1/SHA256.txt) | Checksums for verifying the downloaded package |

For future versions, visit [all releases](https://github.com/Suicideboyy/DiscForge-CHD/releases) or the [latest stable release](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest). Extract the entire Windows ZIP, keep its folders together and launch `DiscForge-CHD.exe`. See the [user guide](USER-GUIDE.txt) for converting PS1 and PS2 games to CHD.

## Versions and branches

| Branch | Version | Purpose |
| --- | --- | --- |
| `master` | **2.6.1** | [Stable download](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest) |
| `dev` | [![dev](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2FSuicideboyy%2FDiscForge-CHD%2Frefs%2Fheads%2Fdev%2Fdev-status.json)](https://github.com/Suicideboyy/DiscForge-CHD/tree/dev) | Development and accumulated RC source checkpoints |

Dev starts from 2.6.1 with no RC or release. Its badge follows the latest development checkpoint. Future changes go to dev; master is merged, compiled and released only on an explicit user request.

DiscForge CHD is a Windows x64 application that converts PlayStation 1 (PS1) and PlayStation 2 (PS2) disc images to the CHD (Compressed Hunks of Data) format using chdman. Convert ISO, BIN/CUE and supported ZIP, RAR or 7z archives through a graphical interface available in English and Brazilian Portuguese.

## Install

Current version: **2.6.1**.

Download the portable ZIP from the [latest release](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest). Extract the entire archive and run `DiscForge-CHD.exe`. Keep every folder from the archive together. .NET and Windows App SDK are included. Windows 10 2004 or newer is required; Windows 11 is recommended. The game cover panel uses the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). Conversion remains available if WebView2 is missing.

- Dark Bento interface inspired by UI/UX Pro Max, with native WinUI 3 controls and contextual help.
- WebView2 game panel, serial-based covers, custom icon and build date. ZIP metadata and covers can load before image extraction when the catalog match is unique.
- PS2 CD: `createcd`, default hunk 2448. DVD: `createdvd`, default hunk 2048. Both are configurable.
- SharpCompress extraction for supported archives, without the 7-Zip application or DLL.
- Game details show release date when supplied by the catalog, current format and current file size; unavailable data is labeled clearly.
- At least one codec is required per applicable media type; missing selections are explained before conversion.
- Compatibility with older devices writes CHD v4 using zlib (CD hunk 9792, DVD hunk 2048). Files may be larger; support depends on device firmware. Current devices should use CHD v5.
- Missing PS1 covers fall back to the Libretro thumbnail collection.
- PS1/PS2 classification from disc boot information before conversion; existing outputs are checked after extraction.
- CHD verification and optional deletion after success.
- Per-entry timer, application and encoder CPU/I/O metrics, and automatic thread count.
- Immediate stop, last-used input folder, default optimized output folder and codec checkboxes.
- Version-aware output names, safe multi-track BIN reconstruction and PS1 covers.
- English and Brazilian Portuguese interface; diagnostic report review and email draft.

## Development

C# 14 / .NET 10. The SDK is pinned in `global.json`; package versions are locked in `fontes/packages.lock.json`. Use `Publish.ps1` for dev updates; stable builds require an explicit user request and `-Stable`. See [source structure](fontes/README.md), [tests](TESTS.txt), [publishing](PUBLISHING.md) and [third-party components](THIRD-PARTY.md).

The default branch is **master**; active development uses **dev**. Previous versions remain in tags. Local distributions are stored under `versions/X.Y.Z/`; downloadable binaries are attached to GitHub Releases. Bug reports: [andrethiesen@live.com](mailto:andrethiesen@live.com).
