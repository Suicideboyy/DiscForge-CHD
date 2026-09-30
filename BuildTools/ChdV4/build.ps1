# DiscForge CHD 2.5.0
param([Parameter(Mandatory)][string]$MameRoot,
      [Parameter(Mandatory)][string]$Compiler,
      [string]$LibraryRoot,
      [string]$Output)
$ErrorActionPreference = 'Stop'
if (!$Output) { $Output = Join-Path $PSScriptRoot '..\..\fontes\tools\chd-v4.exe' }
if (!$LibraryRoot) {
    $LibraryRoot = Join-Path $MameRoot 'build-zen3\mingw-gcc\bin\x64\Release'
}
$libraries = 'utils','expat','7z','ocore_windows','zlib','zstd','flac','utf8proc'
$arguments = @('-std=c++20','-O3','-m64','-march=znver3','-mtune=znver3',
    '-flto=auto','-static','-municode','-DNDEBUG','-DFLAC__NO_DLL',
    '-DWIN32','-DLSB_FIRST',"-I$MameRoot\src\osd",
    "-I$MameRoot\src\lib\util","-I$MameRoot\3rdparty",
    "-I$MameRoot\3rdparty\flac\include", "$PSScriptRoot\main.cpp",
    '-o',$Output,'-Wl,--start-group')
$arguments += $libraries | ForEach-Object { Join-Path $LibraryRoot "lib$_.a" }
$arguments += @('-luser32','-lwinmm','-ladvapi32','-lshlwapi','-lwsock32',
    '-lws2_32','-lpsapi','-liphlpapi','-lshell32','-luserenv','-Wl,--end-group')
$env:PATH = (Split-Path $Compiler) + ';' + $env:PATH
& $Compiler @arguments
if ($LASTEXITCODE) { throw 'CHD v4 converter build failed.' }
