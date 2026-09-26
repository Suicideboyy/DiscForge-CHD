# Compilar e publicar

Branch única: master. Remoto: https://github.com/Suicideboyy/DiscForge-CHD.

1. Instale o SDK .NET 10.0.401 (global.json) e GitHub CLI; autentique com gh auth login.
2. Atualize AssemblyInfo.cs, AppInfo.cs, AppChangelog.cs e RELEASE.md. Valide a mudança.
3. Execute `powershell -NoProfile -ExecutionPolicy Bypass -File .\Publicar.ps1`.

Para só compilar: acrescente `-SomenteCompilar`. SDK portátil: configure DOTNET_ROOT;
GitHub CLI portátil: configure GH_PATH com o caminho do gh.exe.

O script restaura versões travadas, compila C# x64, inclui os runtimes .NET/Windows App SDK,
e cria versoes/X.Y.Z/aplicativo, ZIP portátil, checksum e ZIP dos fontes.
Registra commit/tag, envia à master e anexa os pacotes à Release correspondente.
Não inclui jogos, caches ou credenciais. O chdman incluído não é recompilado.

Tags publicadas nunca são sobrescritas. Se o envio falhar após o commit/tag, retome
o push desses mesmos objetos. Se apenas a Release falhar, retome o upload com gh release.
Não use push forçado. Novas branches não são apagadas automaticamente pelo script.
A consolidação solicitada para esta migração é uma operação única, após preservar o histórico.

O changelog visível no aplicativo contém apenas bugs e funções desde 1.2.0.
Mudanças internas ficam no CHANGELOG.txt e são informadas ao usuário.

Pacotes: DiscForge-CHD-X.Y.Z-win-x64.zip e DiscForge-CHD-X.Y.Z-fontes.zip.
Projeto: fontes/DiscForge-CHD.csproj. O executável da raiz inicia app/DiscForge-CHD.exe.
BuildTools/Launcher compartilha o runtime incluído; HostPatcher usa o SDK oficial
para criar esse iniciador com o ícone. Documentos e licenças têm pastas próprias.
