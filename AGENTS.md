# DiscForge CHD

The user has authorized automatic publication of project updates. After implementing and validating a new application version, update AssemblyVersion and AssemblyFileVersion together, the visible version and app changelog, then run Publish.ps1. Use the existing origin remote and master branch. Never force push or overwrite a release tag. If origin is absent, ask for its URL.

Do not publish changes that fail appropriate checks. Do not include games, temporary files, credentials or other projects. Do not disable antivirus protection or restore quarantined files. Report code changes concisely. Read and edit only necessary modules.

The in-app changelog (Properties/AppChangelog.cs) contains only features and bug fixes since C# 1.2.0. Keep refactoring and formatting out of it. Keep local distributions under versions/X.Y.Z, with the executable, checksum and complete archive. Publish complete portable archives through GitHub Releases, not new binaries at repository root.

The application uses .NET 10/C# 14, WinUI 3 and WebView2. Since 2.4.0, SharpCompress is the only extractor; do not restore 7z.exe or 7z.dll dependencies. Archive extraction uses disk-backed temporary files: chdman requires seekable image files. Do not expose RAM extraction unless genuine in-process memory support is implemented; do not install RAM disk drivers. Preserve the app, documentation and licenses directories in portable archives. Historical WinForms code remains available in Git history.

Documentation-only and repository-cleanup commits do not require recompilation or an application version bump. Publish those commits to master without replacing an existing release.
