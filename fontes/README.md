# Source structure: C# 14 / .NET 10

`Application/Program.cs` starts diagnostics or the WinUI application. `App.xaml` registers resources; the window is built in C#.

| Directory | Responsibility |
|---|---|
| Desktop | WinUI layout, settings, contextual help, conversion events, telemetry and WebView2 panel |
| Processing | Batch control, encoding, verification, resume and reports |
| Archives | SharpCompress extraction and 7-Zip fallback |
| Infrastructure | Processes, JSON, HTTP, system counters and bundled tools |
| Services | Database lookup, cache and covers without UI dependencies |
| Media / Storage | Disc images, CUE, serials, output names and file integrity |
| Configuration / Models | Validated settings and shared data |
| Properties | Version, build date and in-app changelog |
| Assets | Application icon in ICO and PNG formats |

The active project is `DiscForge-CHD.csproj`. `Desktop` separates layout, controls, settings, events, processing, telemetry, About and diagnostics. `VisualTheme` centralizes appearance and `OptionHelp` centralizes explanations. `BuildTools` at repository root contains the portable launcher and host generator. There is no WinForms or System.Web dependency in the compiled application.

Diagnostic commands: `--run-test input output PS2`, `--stop-test`, `--delete-test`, `--archive-test archive destination`, `--cover-test serial file`, `--ui-smoke report`. Use disposable directories for deletion tests.

`build.ps1` creates the versioned package. Update AppInfo.cs and AssemblyInfo.cs together for a new application release. BuildInfo.cs is generated during compilation.
