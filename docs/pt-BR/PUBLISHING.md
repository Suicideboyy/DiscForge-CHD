# Desenvolvimento e publicação estável

[English](../../PUBLISHING.md)

A `master` mantém a versão estável. Todas as alterações futuras vão para `dev`, inicialmente baseada na 2.6.1 e sem RC ou release.

1. Trabalhe na `dev` e execute as verificações adequadas.
2. Para uma nova versão do aplicativo, atualize as versões do assembly e da interface, histórico, notas. Mantenha a mesma versão nas RCs seguintes.
3. Execute `./Publish.ps1 -RC`: registra o código, envia para dev e cria a próxima tag `vX.Y.Z-rc.N`. Não compila nem cria GitHub Release. O indicador da página inicial acompanha `dev-status.json` na dev.
4. Para documentação ou manutenção sem RC, execute `./Publish.ps1 -Message 'Descrição da alteração'` na dev.

Somente após comando explícito do usuário, entre na `master` limpa e execute `./Publish.ps1 -Stable`. O script incorpora todas as alterações de origin/dev, compila e publica a versão estável. `-Stable -BuildOnly` incorpora e compila sem publicar. Volte à dev depois. Não execute o compilador diretamente para contornar essa autorização.

Use o SDK definido em global.json e GitHub CLI autenticado. Os pacotes completos ficam em versions/X.Y.Z, com ZIP portátil, código-fonte e SHA-256. Nunca sobrescreva tags nem use force push. Caso um envio falhe, retome o mesmo commit/tag; caso apenas a release falhe, retome os anexos. Não inclua jogos, caches ou credenciais. Contato: [andrethiesen@live.com](mailto:andrethiesen@live.com).
