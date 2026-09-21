$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Instale o .NET Framework 4.8 para compilar.' }
$date = Get-Date -Format 'dd/MM/yyyy'
$buildInfo = @"
static class BuildInfo
{
    public const string Date = "$date";
}
"@
[IO.File]::WriteAllText(
    (Join-Path $PSScriptRoot 'Properties\BuildInfo.cs'),
    $buildInfo + [Environment]::NewLine)

$sources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -Recurse |
    Sort-Object FullName | ForEach-Object { $_.FullName })

$options = @(
    '/nologo', '/target:winexe', '/platform:x64', '/optimize+',
    '/out:..\CHD-Optimizer.exe',
    '/reference:System.Windows.Forms.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Web.Extensions.dll',
    '/resource:tools\chdman.exe,chdman.exe',
    '/resource:tools\7z.exe,7z.exe',
    '/resource:tools\7z.dll,7z.dll'
)
& $compiler @options @sources
if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação.' }
Get-FileHash -LiteralPath '..\CHD-Optimizer.exe' -Algorithm SHA256
