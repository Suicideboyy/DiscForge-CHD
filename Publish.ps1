param([switch]$RC, [switch]$Stable, [switch]$BuildOnly,
    [string]$Message = 'Update DiscForge CHD development')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
function Git {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
    $result = & git.exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git failed: $($Arguments -join ' '). No force push was used." }
    return $result
}
$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fontes\Properties\AssemblyInfo.cs'))
$version = [regex]::Match($source, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)').Groups[1].Value
if (-not $version) { throw 'Invalid version.' }
$tag = 'v' + $version
$folder = Join-Path $PSScriptRoot ('versions\' + $version)
if (($RC -and $Stable) -or ($BuildOnly -and -not $Stable)) {
    throw 'Use -RC for development, or -Stable [-BuildOnly] after explicit user authorization.'
}
if (-not $Stable) {
    if ((Git symbolic-ref --short HEAD) -ne 'dev') { throw 'Development updates require dev.' }
    Git fetch origin --prune | Out-Null
    Git merge --ff-only origin/dev | Out-Null
    if ($RC) {
        if (Git tag --list $tag) { throw 'Increment the application version before its first RC.' }
        $numbers = @(Git tag --list "v$version-rc.*" | ForEach-Object {
            if ($_ -match '-rc\.(\d+)$') { [int]$Matches[1] }
        })
        $next = if ($numbers.Count) { ($numbers | Measure-Object -Maximum).Maximum + 1 } else { 1 }
        $tag = "v$version-rc.$next"
        @{ schemaVersion = 1; label = 'dev'; message = "$version-rc.$next"; color = 'orange' } |
            ConvertTo-Json | Set-Content -LiteralPath 'dev-status.json' -Encoding UTF8
    }
    Git add -u -- .
    Git add -- fontes BuildTools docs design-system Publish.ps1 dev-status.json global.json .gitignore .editorconfig AGENTS.md README.md PUBLISHING.md USER-GUIDE.txt CHANGELOG.txt TESTS.txt THIRD-PARTY.md RELEASE.md
    if (Git diff --cached --name-only) {
        Git commit -m $(if ($RC) { "DiscForge CHD $tag" } else { $Message })
    } elseif ($RC) { throw 'No changes to create an RC checkpoint.' }
    if ($RC) {
        Git tag $tag
        Git push --atomic origin HEAD:refs/heads/dev "refs/tags/$tag"
    } else { Git push origin HEAD:refs/heads/dev }
    Write-Host 'Development source pushed. No compilation or GitHub Release.'
    return
}
if ((Git symbolic-ref --short HEAD) -ne 'master') { throw 'Stable builds require master.' }
if (Git status --porcelain) { throw 'Commit and push development changes before promotion.' }
Git fetch origin --prune | Out-Null
Git merge --ff-only origin/master | Out-Null
Git merge --no-ff origin/dev -m 'Promote development to master' | Out-Null
$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fontes\Properties\AssemblyInfo.cs'))
$version = [regex]::Match($source, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)').Groups[1].Value
if (-not $version) { throw 'Invalid promoted version.' }
$tag = 'v' + $version
$folder = Join-Path $PSScriptRoot ('versions\' + $version)
if (-not $BuildOnly) {
    if ((Git symbolic-ref --short HEAD) -ne 'master') { throw 'Publish only from master.' }
    if (Git tag --list $tag) { throw 'This version already has a local tag. Resume publication or increment the version.' }
    Git fetch origin --prune | Out-Null
    if (Git ls-remote --heads origin master) { Git merge --ff-only origin/master | Out-Null }
    if (Git ls-remote --tags origin "refs/tags/$tag") { throw 'Tag already published.' }
    $gh = if ($env:GH_PATH) { $env:GH_PATH } else { (Get-Command gh -ErrorAction Stop).Source }
    & $gh auth status
    if ($LASTEXITCODE -ne 0) { throw 'Authenticate GitHub CLI before publishing.' }
}
foreach ($file in @('README.md', 'docs/pt-BR/README.md')) {
    $path = Join-Path $PSScriptRoot $file
    $text = [IO.File]::ReadAllText($path)
    $text = [regex]::Replace($text, '(?m)(\| `master` \| \*\*)\d+\.\d+\.\d+(\*\*)', "`${1}$version`${2}")
    $text = [regex]::Replace($text, '((?:Current version|Versão atual): \*\*)\d+\.\d+\.\d+(\*\*)', "`${1}$version`${2}")
    [IO.File]::WriteAllText($path, $text)
}
& (Join-Path $PSScriptRoot 'fontes\build.ps1')
if ($BuildOnly) { return }
# Application code, documentation and tools only; exclude games and caches.
Git add -u -- .
Git add -- fontes BuildTools docs design-system Publish.ps1 global.json .gitignore .editorconfig AGENTS.md README.md PUBLISHING.md USER-GUIDE.txt CHANGELOG.txt TESTS.txt THIRD-PARTY.md RELEASE.md
if (Git diff --cached --name-only) { Git commit -m "DiscForge CHD $tag" }
Git tag $tag
$sources = Join-Path $folder ('DiscForge-CHD-' + $version + '-source.zip')
Git archive --format=zip --output $sources HEAD
Git push --atomic origin HEAD:refs/heads/master "refs/tags/$tag"
$package = Join-Path $folder ('DiscForge-CHD-' + $version + '-win-x64.zip')
& $gh release create $tag $package $sources (Join-Path $folder 'SHA256.txt') --repo Suicideboyy/DiscForge-CHD --title "DiscForge CHD $version" --notes-file (Join-Path $PSScriptRoot 'RELEASE.md')
if ($LASTEXITCODE -ne 0) { throw 'Commit and tag were pushed, but release creation failed. Resume only the asset upload.' }
Write-Host "Published: $tag"
