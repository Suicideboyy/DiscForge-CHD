param([switch]$BuildOnly)
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
& (Join-Path $PSScriptRoot 'fontes\build.ps1')
if ($BuildOnly) { return }
# Application code, documentation and tools only; exclude games and caches.
Git add -u -- .
Git add -- fontes BuildTools Publish.ps1 global.json .gitignore .editorconfig AGENTS.md README.md PUBLISHING.md USER-GUIDE.txt CHANGELOG.txt TESTS.txt THIRD-PARTY.md RELEASE.md
Git commit -m "DiscForge CHD $tag"
Git tag $tag
$sources = Join-Path $folder ('DiscForge-CHD-' + $version + '-source.zip')
Git archive --format=zip --output $sources HEAD
Git push --atomic origin HEAD:refs/heads/master "refs/tags/$tag"
$package = Join-Path $folder ('DiscForge-CHD-' + $version + '-win-x64.zip')
& $gh release create $tag $package $sources (Join-Path $folder 'SHA256.txt') --repo Suicideboyy/DiscForge-CHD --title "DiscForge CHD $version" --notes-file (Join-Path $PSScriptRoot 'RELEASE.md')
if ($LASTEXITCODE -ne 0) { throw 'Commit and tag were pushed, but release creation failed. Resume only the asset upload.' }
Write-Host "Published: $tag"
