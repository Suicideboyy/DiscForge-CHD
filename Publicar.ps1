param([switch]$SomenteCompilar)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Git {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
    $result = & git.exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git falhou: $($Arguments -join ' '). Nenhum push forçado foi realizado." }
    return $result
}

$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fontes\Properties\AssemblyInfo.cs'))
$match = [regex]::Match($source, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)')
if (-not $match.Success) { throw 'Use AssemblyVersion no formato X.Y.Z.0.' }
$tag = 'v' + $match.Groups[1].Value
$versionFolder = Join-Path $PSScriptRoot ('versoes\' + $match.Groups[1].Value)

if (-not $SomenteCompilar) {
    if (-not (Test-Path -LiteralPath '.git')) { throw 'Configure primeiro o repositório Git e o remoto origin.' }
    $remote = Git remote get-url origin
    $branch = Git symbolic-ref --short HEAD
    Git check-ref-format --branch $branch | Out-Null
    if (Git tag --list $tag) { throw "A versão $tag já foi registrada. Aumente a versão antes de publicar uma atualização." }
    $remoteTag = Git ls-remote --tags origin "refs/tags/$tag"
    if ($remoteTag) { throw "A versão $tag já existe no servidor. Aumente a versão." }
    $staged = @(Git diff --cached --name-only)
    if ($staged.Count -gt 0) { throw 'Existem alterações preparadas no Git. Resolva-as antes da publicação automática.' }
}

# PowerShell é usado apenas na compilação/publicação, nunca pelo aplicativo.
& (Join-Path $PSScriptRoot 'fontes\compilar.ps1')
Set-Location -LiteralPath $PSScriptRoot
if (-not (Test-Path -LiteralPath 'CHD-Optimizer.exe')) { throw 'Executável não foi gerado.' }
$hash = Get-FileHash -LiteralPath 'CHD-Optimizer.exe' -Algorithm SHA256
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'SHA256.txt'), $hash.Hash + '  CHD-Optimizer.exe' + [Environment]::NewLine)
Copy-Item -LiteralPath 'SHA256.txt', 'LEIA-ME.txt', 'CHANGELOG.txt', 'TESTES.txt' -Destination $versionFolder -Force
if ($SomenteCompilar) { Write-Host 'Compilado; nenhuma publicação solicitada.'; exit 0 }

# Lista explícita: não inclui a coleção de jogos, caches ou credenciais.
Git add -- fontes Publicar.ps1 .gitignore .editorconfig AGENTS.md README.md PUBLICACAO.md LEIA-ME.txt CHANGELOG.txt TESTES.txt CHD-Optimizer.exe SHA256.txt
Git commit -m "CHD Optimizer $tag"
Git tag $tag
Git archive --format=zip --output (Join-Path $versionFolder ('CHD-Optimizer-' + $match.Groups[1].Value + '-completo.zip')) HEAD
Git push --atomic origin "HEAD:refs/heads/$branch" "refs/tags/$tag"
Write-Host "Publicado: $tag ($remote)."
