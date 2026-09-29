# Compilação e publicação

[English](../../PUBLISHING.md)

Use a branch `master`. Instale o SDK .NET definido em `global.json` e autentique o GitHub CLI. Para uma nova versão, atualize `AssemblyInfo.cs`, `AppInfo.cs`, o histórico do aplicativo e as notas da versão. Execute `powershell -NoProfile -ExecutionPolicy Bypass -File .\Publish.ps1`. A opção `-BuildOnly` compila sem publicar.

O script cria `versions/X.Y.Z/application`, ZIP portátil, código-fonte e SHA-256. Depois registra commit e tag, envia para `master` e cria uma GitHub Release. Nunca sobrescreva tags nem use force push. O pacote não inclui jogos, caches ou credenciais. Reporte erros para [andrethiesen@live.com](mailto:andrethiesen@live.com).
