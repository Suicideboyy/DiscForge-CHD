param([switch]$SomenteCompilar)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
function Git {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
    $result = & git.exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git falhou: $($Arguments -join ' '). Não foi usado push forçado." }
    return $result
}
$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fontes\Properties\AssemblyInfo.cs'))
$version = [regex]::Match($source, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)').Groups[1].Value
if (-not $version) { throw 'Versão inválida.' }
$tag = 'v' + $version
$folder = Join-Path $PSScriptRoot ('versoes\' + $version)
if (-not $SomenteCompilar) {
    if ((Git symbolic-ref --short HEAD) -ne 'master') { throw 'Publique somente pela master.' }
    if (Git tag --list $tag) { throw 'Esta versão já foi registrada. Retome o envio ou incremente a versão.' }
    Git fetch origin --prune | Out-Null
    if (Git ls-remote --heads origin master) { Git merge --ff-only origin/master | Out-Null }
    if (Git ls-remote --tags origin "refs/tags/$tag") { throw 'Tag já publicada.' }
    $gh = if ($env:GH_PATH) { $env:GH_PATH } else { (Get-Command gh -ErrorAction Stop).Source }
    & $gh auth status
    if ($LASTEXITCODE -ne 0) { throw 'Autentique o GitHub CLI antes de publicar.' }
}
& (Join-Path $PSScriptRoot 'fontes\compilar.ps1')
if ($SomenteCompilar) { return }
# Apenas código, documentação e ferramentas do aplicativo; jogos/caches ficam fora.
Git add -- fontes BuildTools Publicar.ps1 global.json .gitignore .editorconfig AGENTS.md README.md PUBLICACAO.md LEIA-ME.txt CHANGELOG.txt TESTES.txt TERCEIROS.md RELEASE.md SHA256.txt
Git commit -m "DiscForge CHD $tag"
Git tag $tag
$sources = Join-Path $folder ('DiscForge-CHD-' + $version + '-fontes.zip')
Git archive --format=zip --output $sources HEAD
Git push --atomic origin HEAD:refs/heads/master "refs/tags/$tag"
$package = Join-Path $folder ('DiscForge-CHD-' + $version + '-win-x64.zip')
& $gh release create $tag $package $sources (Join-Path $folder 'SHA256.txt') --repo Suicideboyy/DiscForge-CHD --title "DiscForge CHD $version" --notes-file (Join-Path $PSScriptRoot 'RELEASE.md')
if ($LASTEXITCODE -ne 0) { throw 'Commit e tag enviados, mas a Release falhou. Retome apenas o upload dos arquivos.' }
Write-Host "Publicado: $tag"
