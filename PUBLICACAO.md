Atualizações do CHD Optimizer

Este projeto reúne o código C#, as ferramentas incluídas e o executável atual.
Repositório público: https://github.com/Suicideboyy/DiscForge-CHD
Remoto Git: `origin`; branch: `main`.

Após alterar o código, atualizar a versão X.Y.Z.0 em fontes/Properties/AssemblyInfo.cs e o changelog,
validar a alteração e executar:

    powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Publicar.ps1

O comando compila, calcula SHA-256, registra a atualização e publica código,
executável e tag vX.Y.Z no remoto origin. Usa a autenticação normal do Git;
não salva senhas ou tokens no projeto. Uma versão já publicada não é sobrescrita.

Para compilar sem publicar, acrescente -SomenteCompilar.

Se o push falhar depois de criar o commit/tag local, corrija o acesso e envie
esse mesmo commit/tag com Git; não crie outra versão apenas para repetir o envio.
Não use push forçado. O servidor precisa aceitar push atômico de branch e tag.

Não há monitoramento recorrente: a publicação ocorre ao concluir cada atualização,
com um comando local, sem precisar consultar um agente periodicamente.

Cada versão fica em versoes/X.Y.Z, com executável, SHA256.txt, documentação
e ZIP completo do commit publicado. A raiz mantém a versão mais recente.
As pastas locais versoes/ não são duplicadas no Git; as tags preservam o histórico.
Atualize também fontes/Properties/AppInfo.cs e AppChangelog.cs. O histórico
visível no aplicativo inclui apenas funções e correções desde 1.2.0.
