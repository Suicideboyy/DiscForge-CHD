# Development and stable publication

Remote: https://github.com/Suicideboyy/DiscForge-CHD. `master` is stable; `dev` receives all future changes. Initially dev is based on 2.6.1 and has no RC or release.

1. Switch to `dev`, implement changes and run appropriate checks.
2. For a new application version, update `AssemblyInfo.cs`, `AppInfo.cs`, `fontes/Assets/changelog.json`, release notes. Keep the same version for successive RCs.
3. Run `./Publish.ps1 -RC` to commit, push dev and tag the next `vX.Y.Z-rc.N`. These are source checkpoints: no executable build and no GitHub Release. The homepage dev badge updates automatically from `dev-status.json` on dev.
4. For documentation or maintenance without a new RC, run `./Publish.ps1 -Message 'Describe the change'` on dev. It commits and pushes source only.

Only after the user explicitly requests promotion/compilation, switch to a clean `master` and run `./Publish.ps1 -Stable`. This fetches, merges all `origin/dev` changes, builds and publishes the new stable version. `-Stable -BuildOnly` performs the authorized merge/build without publishing. Return to dev afterward. Calling the build script directly does not bypass the authorization requirement.

Stable builds require the SDK in `global.json` and authenticated GitHub CLI; portable tools may set `DOTNET_ROOT` and `GH_PATH`. Packages are restored from the lock file. Outputs remain in `versions/X.Y.Z/`: complete x64 portable ZIP, SHA-256 and source ZIP. The package includes the launcher, app, documentation and licenses; chdman is not rebuilt.

Never overwrite tags or force push. If pushing fails, resume the existing commit/tag rather than creating another RC. If release creation fails after pushing, resume asset upload only. Keep games, caches and credentials outside Git. The app history lists features and bug fixes only; internal maintenance belongs in repository commits.
