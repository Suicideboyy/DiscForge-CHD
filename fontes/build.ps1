param([string]$DotnetPath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $DotnetPath) {
    $found = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($found) { $DotnetPath = $found.Source }
    elseif ($env:DOTNET_ROOT) { $DotnetPath = Join-Path $env:DOTNET_ROOT 'dotnet.exe' }
    else { throw 'Install .NET SDK 10.0.401 or supply -DotnetPath.' }
}
$assembly = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Properties\AssemblyInfo.cs'))
$version = [regex]::Match($assembly, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)').Groups[1].Value
if (-not $version) { throw 'Invalid version.' }
$folder = Join-Path $repo ('versions\' + $version)
$portable = Join-Path $folder 'application'
$application = Join-Path $portable 'app'
$documentation = Join-Path $portable 'documentation'
New-Item -ItemType Directory -Path $application,$documentation -Force | Out-Null
$date = Get-Date -Format 'dd/MM/yyyy'
$buildInfo = "static class BuildInfo { public const string Date = `"$date`"; }"
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'Properties\BuildInfo.cs'), $buildInfo + [Environment]::NewLine)
Push-Location $repo
try {
    & $DotnetPath publish (Join-Path $PSScriptRoot 'DiscForge-CHD.csproj') -c Release -r win-x64 --self-contained true -o $application -p:RestoreLockedMode=true -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    # The launcher shares the runtime in app, where WinUI must reside beside its native DLLs.
    & $DotnetPath publish (Join-Path $repo 'BuildTools\Launcher\Launcher.csproj') -c Release -o $application -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }
    $hostTool = Join-Path $repo 'BuildTools\HostPatcher\HostPatcher.csproj'
    & $DotnetPath build $hostTool -c Release -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Host generator build failed.' }
    $runtime = Get-Content (Join-Path $application 'DiscForge-CHD.runtimeconfig.json') -Raw | ConvertFrom-Json
    $runtimeVersion = $runtime.runtimeOptions.includedFrameworks[0].version
    $template = Join-Path (Split-Path -Parent $DotnetPath) (
        'packs\Microsoft.NETCore.App.Host.win-x64\' + $runtimeVersion + '\runtimes\win-x64\native\apphost.exe')
    $hostDll = Join-Path $repo 'BuildTools\HostPatcher\bin\Release\net10.0\HostPatcher.dll'
    & $DotnetPath $hostDll $template (Join-Path $portable 'DiscForge-CHD.exe') 'app/DiscForge.Launcher.dll' (Join-Path $application 'DiscForge.Launcher.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Failed to generate the root executable.' }
    Copy-Item -LiteralPath 'USER-GUIDE.txt' -Destination $portable -Force
    Copy-Item -LiteralPath 'CHANGELOG.txt','TESTS.txt','THIRD-PARTY.md' -Destination $documentation -Force
    $licenses = Join-Path $portable 'licenses'
    New-Item -ItemType Directory -Path $licenses -Force | Out-Null
    Copy-Item -Path (Join-Path $PSScriptRoot 'licenses\*') -Destination $licenses -Force
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
    $zip = Join-Path $folder ('DiscForge-CHD-' + $version + '-win-x64.zip')
    $candidate = Join-Path $folder ('package-' + [Guid]::NewGuid().ToString('N') + '.zip')
    $sevenZip = Join-Path $PSScriptRoot 'tools\7z.exe'
    Push-Location $portable
    try {
        & $sevenZip a -tzip -mx=5 -ssw -sse $candidate '.\*' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Failed to package the distribution.' }
        & $sevenZip t $candidate | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Invalid ZIP package.' }
    } finally { Pop-Location }
    Move-Item -LiteralPath $candidate -Destination $zip -Force
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
    [IO.File]::WriteAllText((Join-Path $folder 'SHA256.txt'), "$hash  $([IO.Path]::GetFileName($zip))`r`n")
    Write-Host "Portable package: $zip"
} finally { Pop-Location }
