param([string]$DotnetPath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $DotnetPath) {
    $found = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($found) { $DotnetPath = $found.Source }
    elseif ($env:DOTNET_ROOT) { $DotnetPath = Join-Path $env:DOTNET_ROOT 'dotnet.exe' }
    else { throw 'Instale o SDK .NET 10.0.401 ou informe -DotnetPath.' }
}
$assembly = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Properties\AssemblyInfo.cs'))
$version = [regex]::Match($assembly, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)').Groups[1].Value
if (-not $version) { throw 'Versão inválida.' }
$folder = Join-Path $repo ('versoes\' + $version)
$portable = Join-Path $folder 'aplicativo'
New-Item -ItemType Directory -Path $portable -Force | Out-Null
$date = Get-Date -Format 'dd/MM/yyyy'
$buildInfo = "static class BuildInfo { public const string Date = `"$date`"; }"
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'Properties\BuildInfo.cs'), $buildInfo + [Environment]::NewLine)
Push-Location $repo
try {
    & $DotnetPath publish (Join-Path $PSScriptRoot 'CHD-Optimizer.csproj') -c Release -r win-x64 --self-contained true -o $portable -p:RestoreLockedMode=true -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação.' }
    Copy-Item -LiteralPath 'LEIA-ME.txt','CHANGELOG.txt','TESTES.txt','TERCEIROS.md' -Destination $portable -Force
    $licenses = Join-Path $portable 'licencas'
    New-Item -ItemType Directory -Path $licenses -Force | Out-Null
    Copy-Item -Path (Join-Path $PSScriptRoot 'licencas\*') -Destination $licenses -Force
    foreach ($notice in Get-ChildItem -LiteralPath (Split-Path -Parent $DotnetPath) -File |
        Where-Object { $_.Name -match '(?i)^license|^third.party.notices' }) {
        Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $licenses ('dotnet-' + $notice.Name)) -Force
    }
    $assets = Get-Content (Join-Path $PSScriptRoot 'obj\project.assets.json') -Raw | ConvertFrom-Json
    foreach ($packageRoot in $assets.packageFolders.PSObject.Properties.Name) {
        foreach ($library in $assets.libraries.PSObject.Properties) {
            if ($library.Value.type -ne 'package') { continue }
            $location = Join-Path $packageRoot $library.Value.path
            foreach ($notice in Get-ChildItem -LiteralPath $location -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -match '(?i)license|notice|third.?party' }) {
                $name = $library.Name.Replace('/','-') + '-' + $notice.Name
                Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $licenses $name) -Force
            }
        }
    }
    $zip = Join-Path $folder ('CHD-Optimizer-' + $version + '-win-x64.zip')
    $candidate = Join-Path $folder ('pacote-' + [Guid]::NewGuid().ToString('N') + '.zip')
    $sevenZip = Join-Path $PSScriptRoot 'tools\7z.exe'
    Push-Location $portable
    try {
        & $sevenZip a -tzip -mx=5 -ssw -sse $candidate '.\*' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao empacotar a distribuição.' }
        & $sevenZip t $candidate | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Pacote ZIP inválido.' }
    } finally { Pop-Location }
    Move-Item -LiteralPath $candidate -Destination $zip -Force
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
    [IO.File]::WriteAllText((Join-Path $folder 'SHA256.txt'), "$hash  $([IO.Path]::GetFileName($zip))`r`n")
    Copy-Item -LiteralPath (Join-Path $folder 'SHA256.txt') -Destination (Join-Path $repo 'SHA256.txt') -Force
    Write-Host "Pacote portátil: $zip"
} finally { Pop-Location }
