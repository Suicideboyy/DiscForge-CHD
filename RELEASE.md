DiscForge CHD 2.4.0

- Removed the 7-Zip executable and DLL from the application and build workflow. SharpCompress handles supported archive formats, including .7z.
- Added optional extraction to an existing verified RAM disk. Requires more than 12 GiB total physical memory and at least 5 GiB available.
- Per-game capacity checks preserve a memory reserve; unavailable or insufficient RAM uses disk with a logged reason. The final CHD is written to disk.
- RAM workspace cleanup deletes only the application's own temporary folder and never unmounts the user's drive. No driver is installed.

Extract the complete DiscForge-CHD-2.4.0-win-x64.zip and run DiscForge-CHD.exe. Windows 10 2004+ x64 is required. Report bugs: andrethiesen@live.com.
