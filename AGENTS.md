# DiscForge CHD

All future changes belong on dev. The user authorizes committing and pushing validated development updates. Promotion to master, executable compilation and stable GitHub Releases require a new explicit user command. Before editing, switch to dev. Never merge automatically, force push or overwrite a tag.

For application changes, update AssemblyVersion/AssemblyFileVersion and the visible version together, then run Publish.ps1 -RC. This accumulates source checkpoints vX.Y.Z-rc.N without compiling or creating GitHub Releases. Further RCs for that version keep the same application version. Documentation-only changes use Publish.ps1 without -RC and need no version bump.

Only after explicit authorization, switch to master and run Publish.ps1 -Stable: it merges all origin/dev changes, compiles and releases. -Stable -BuildOnly merges and compiles without publishing. Stable version rows in both READMEs are updated automatically during promotion. Return to dev afterward. Never invoke fontes/build.ps1 to bypass authorization. If origin is absent, ask for its URL.

Read and edit only necessary modules, run appropriate checks, and report additions/deletions/changes by file. Do not include games, temporary files, credentials or unrelated projects. Do not disable antivirus protection or restore quarantined files.

The in-app history (fontes/Assets/changelog.json) includes only features and bug fixes since C# 1.2.0; exclude refactoring and formatting. Keep authorized local distributions under versions/X.Y.Z with the complete portable archive and checksum. Publish binaries through GitHub Releases, never at repository root.

The application uses .NET 10/C# 14, WinUI 3 and WebView2. SharpCompress is the only extractor; do not restore 7z.exe/7z.dll dependencies. Extraction uses disk-backed temporaries because chdman requires seekable images. Do not expose RAM extraction without genuine in-process support or install RAM disk drivers. Preserve app, documentation and licenses in portable archives.
