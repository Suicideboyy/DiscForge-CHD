# DiscForge CHD — Conversor de PS1 e PS2 para CHD no Windows

[English](../../README.md) · [Relatar erro](mailto:andrethiesen@live.com)

## Baixar DiscForge CHD para Windows — conversor de PS1 e PS2

Converta imagens ISO e BIN/CUE de PlayStation para CHD pela interface gráfica. Baixe o pacote portátil completo; não é necessário instalar o .NET separadamente.

| Download | Conteúdo |
| --- | --- |
| **[Baixar DiscForge CHD 2.6.1 — Windows x64](https://github.com/Suicideboyy/DiscForge-CHD/releases/download/v2.6.1/DiscForge-CHD-2.6.1-win-x64.zip)** | Aplicativo completo, encoder e arquivos necessários para executar |
| [Baixar código-fonte — 2.6.1](https://github.com/Suicideboyy/DiscForge-CHD/releases/download/v2.6.1/DiscForge-CHD-2.6.1-source.zip) | Código da versão estável |
| [Baixar verificações SHA-256](https://github.com/Suicideboyy/DiscForge-CHD/releases/download/v2.6.1/SHA256.txt) | Valores para conferir a integridade do pacote baixado |

Para próximas versões, acesse [todas as versões](https://github.com/Suicideboyy/DiscForge-CHD/releases) ou a [última versão estável](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest). Extraia todo o ZIP do Windows, mantenha as pastas juntas e execute `DiscForge-CHD.exe`. Consulte o [guia de uso](USER-GUIDE.txt) para converter jogos de PS1 e PS2 para CHD.

## Versões e branches

| Branch | Versão | Finalidade |
| --- | --- | --- |
| `master` | **2.7.0** | [Download estável](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest) |
| `dev` | [![dev](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2FSuicideboyy%2FDiscForge-CHD%2Frefs%2Fheads%2Fdev%2Fdev-status.json)](https://github.com/Suicideboyy/DiscForge-CHD/tree/dev) | Desenvolvimento e RCs acumuladas do código |

A dev começa baseada na 2.6.1, sem RC ou release. O indicador acompanha a RC mais recente. Alterações futuras ficam na dev; incorporar, compilar e publicar pela master exige comando explícito do usuário.

Versão atual: **2.6.1**.

O painel do jogo mostra data de lançamento quando informada pelo catálogo, formato e tamanho atuais. Informações indisponíveis são identificadas. A extração utiliza temporários em disco; a opção de unidade RAM foi removida. Codecs ausentes recebem uma explicação antes da conversão. Capas de PS1 podem usar o acervo Libretro como reserva.

**Compatibilidade com dispositivos antigos** grava CHD v4 com zlib, hunk de CD de 9792 bytes e DVD de 2048 bytes. Os arquivos podem ficar maiores; o suporte depende do firmware do dispositivo. Em dispositivos atuais, prefira CHD v5.

Aplicativo Windows x64 para converter jogos de PlayStation 1 e 2 para CHD. Baixe o [ZIP da versão mais recente](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest), extraia tudo e execute `DiscForge-CHD.exe`. Requer Windows 10 2004 ou superior. .NET 10 e Windows App SDK estão incluídos; WebView2 é necessário apenas para exibir capas.

O programa extrai ZIP, RAR e outros formatos somente com SharpCompress. Inspeciona as informações de inicialização do disco para identificar PS1 ou PS2. A detecção automática escolhe o perfil adequado; no modo manual, a conversão é recusada se o sistema não corresponder à seleção. Faixas BIN numeradas sem CUE podem receber um CUE temporário quando a estrutura é segura de inferir. Os arquivos originais são preservados nesse caso.

Selecione até quatro codecs por tipo de mídia. Para CD de PS2, o padrão é `createcd` e hunk de 2448 bytes; para DVD, `createdvd` e hunk de 2048 bytes. As opções têm explicações no aplicativo. O destino padrão é a subpasta `otimizados` da pasta de entrada. Capas de PS1 e PS2, medidores de CPU/disco e parada imediata estão disponíveis.

O botão **Relatar erro** cria um ZIP de até 10 MB com logs recentes e abre um rascunho de e-mail com o anexo para revisão. Nada é enviado automaticamente. Contato: [andrethiesen@live.com](mailto:andrethiesen@live.com).

Veja o [guia de uso](USER-GUIDE.txt), [publicação](PUBLISHING.md), [estrutura do código](SOURCE.md), [componentes de terceiros](THIRD-PARTY.md) e [notas da versão](RELEASE.md).
