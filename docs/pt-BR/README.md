# DiscForge CHD

[English](../../README.md) · [Relatar erro](mailto:andrethiesen@live.com)

Aplicativo Windows x64 para converter jogos de PlayStation 1 e 2 para CHD. Baixe o [ZIP da versão mais recente](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest), extraia tudo e execute `DiscForge-CHD.exe`. Requer Windows 10 2004 ou superior. .NET 10 e Windows App SDK estão incluídos; WebView2 é necessário apenas para exibir capas.

O programa extrai ZIP, RAR e outros formatos com SharpCompress e usa 7-Zip como reserva. Inspeciona as informações de inicialização do disco para identificar PS1 ou PS2. A detecção automática escolhe o perfil adequado; no modo manual, a conversão é recusada se o sistema não corresponder à seleção. Faixas BIN numeradas sem CUE podem receber um CUE temporário quando a estrutura é segura de inferir. Os arquivos originais são preservados nesse caso.

Selecione até quatro codecs por tipo de mídia. Para CD de PS2, o padrão é `createcd` e hunk de 2448 bytes; para DVD, `createdvd` e hunk de 2048 bytes. As opções têm explicações no aplicativo. O destino padrão é a subpasta `otimizados` da pasta de entrada. Capas de PS1 e PS2, medidores de CPU/disco e parada imediata estão disponíveis.

O botão **Relatar erro** cria um ZIP de até 10 MB com logs recentes e abre um rascunho de e-mail com o anexo para revisão. Nada é enviado automaticamente. Contato: [andrethiesen@live.com](mailto:andrethiesen@live.com).

Veja o [guia de uso](USER-GUIDE.txt), [publicação](PUBLISHING.md), [estrutura do código](SOURCE.md), [componentes de terceiros](THIRD-PARTY.md) e [notas da versão](RELEASE.md).
