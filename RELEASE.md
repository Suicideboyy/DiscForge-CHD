DiscForge CHD 2.5.0

- RAM extraction checkbox follows physical memory eligibility (>12 GiB total, >=5 GiB free). Clear guidance distinguishes available memory from an existing RAM drive; invalid/missing drives fall back to disk with a reason.
- Missing CD/DVD codec selections now receive an explanation before processing.
- PS1 cover art can fall back to the Libretro thumbnail repository.
- Compatibility with older devices writes CHD v4 using zlib, CD hunks of 9792 bytes and DVD hunks of 2048 bytes. Output may be larger; support depends on device firmware.

Extract the complete DiscForge-CHD-2.5.0-win-x64.zip and run DiscForge-CHD.exe. Windows 10 2004+ x64 is required. Report bugs: andrethiesen@live.com.