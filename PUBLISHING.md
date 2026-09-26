# Build and publish

Branch: master. Remote: https://github.com/Suicideboyy/DiscForge-CHD.

1. Install .NET SDK 10.0.401 (see global.json) and authenticate GitHub CLI.
2. For a new application version, update AssemblyInfo.cs, AppInfo.cs, AppChangelog.cs and RELEASE.md. Validate the change.
3. Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\Publish.ps1`.

Use `-BuildOnly` to build without publishing. For portable tools, set `DOTNET_ROOT` and `GH_PATH`. The scripts restore locked packages, compile x64, include the .NET and Windows App SDK runtimes, and create `versions/X.Y.Z/application`, the portable ZIP, a SHA-256 checksum and a source ZIP. The release script commits and tags the source, pushes master and attaches the archives to a GitHub Release. It does not include games, cache or credentials; the bundled chdman is not rebuilt.

Release tags are never overwritten. If a push fails after a local commit and tag, resume pushing those objects. If only release creation fails, resume asset upload with GitHub CLI. Never force push.

The portable archive contains a root `DiscForge-CHD.exe` launcher and `app`, `documentation` and `licenses` directories. The launcher shares the packaged runtime. Source archives and portable archives are named `DiscForge-CHD-X.Y.Z-source.zip` and `DiscForge-CHD-X.Y.Z-win-x64.zip`.

The in-app changelog includes only features and bug fixes since version 1.2.0. Internal changes belong in CHANGELOG.txt. Documentation-only commits go directly to master without recompiling or reissuing a release.
