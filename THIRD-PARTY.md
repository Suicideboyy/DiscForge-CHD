# Third-party components

DiscForge CHD 2.6.2.

- Libretro PS1 thumbnails: https://github.com/libretro-thumbnails/Sony_-_PlayStation — online cover fallback; cover art is not bundled.

- .NET 10 / SDK 10.0.401: https://github.com/dotnet/runtime — MIT and component notices.
- Windows App SDK 2.5.1 / WinUI 3: https://github.com/microsoft/WindowsAppSDK — Microsoft/MIT licenses.
- WebView2 SDK 1.0.4191.47: https://www.nuget.org/packages/Microsoft.Web.WebView2 — Microsoft license. The Evergreen Runtime is installed and updated separately by Microsoft.
- SharpCompress 1.0.0: https://github.com/adamhathcock/sharpcompress — MIT.
- Plotly.js 4.1.2 (basic bundle, local file `fontes/Assets/Plotly/plotly-basic.min.js`): https://github.com/plotly/plotly.js — MIT. Offline telemetry sparklines only; no CDN, no server.
- chdman/MAME: https://github.com/mamedev/mame — licenses vary by MAME component. The user-provided custom 0.289 / Zen 3 binary, built on 2026-09-14, is preserved.
- PS2 covers: https://github.com/xlenore/ps2-covers — downloaded by serial.
- PS1 covers: https://github.com/xlenore/psx-covers — downloaded by serial as PNG.
  Covers are not distributed in the archive.

NuGet versions are locked in packages.lock.json. Component license texts and notices are included in the portable distribution's licenses directory. No game or real disc image belongs to this repository.
