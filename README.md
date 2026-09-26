# DiscForge CHD

Aplicativo Windows x64 para converter jogos PS1/PS2 em CHD.

## Instalar

Baixe o pacote **win-x64.zip** na [última Release](https://github.com/Suicideboyy/DiscForge-CHD/releases/latest),
extraia a pasta inteira e abra `DiscForge-CHD.exe`. Preserve as subpastas `app`, `documentacao` e `licencas`.
.NET e Windows App SDK estão incluídos. Windows 10 2004 ou posterior; Windows 11 recomendado.
O painel de capas utiliza o [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).
Se ele faltar, o aplicativo informa e mantém a conversão disponível.

- Interface WinUI 3 com cartões, ajustes avançados recolhíveis e ajuda em cada opção.
- Painel do jogo em WebView2, capas por serial, ícone próprio e data da compilação.
- PS2 CD: createcd / hunk 2448; DVD: createdvd / hunk 2048. Opções ajustáveis.
- SharpCompress como extrator principal; 7-Zip de reserva para formatos/métodos incompatíveis e volumes.
- Retomada antes de extrair novamente, verificação de CHD e exclusão opcional após sucesso.
- Tempo por entrada, CPU e discos do sistema, threads automáticas.

## Desenvolvimento

C# 14 / .NET 10. SDK fixado em global.json; dependências fixadas em fontes/packages.lock.json.
Execute `fontes/compilar.ps1` com o SDK instalado. Leia [estrutura dos fontes](fontes/README.md),
[testes](TESTES.txt) e [publicação](PUBLICACAO.md).

A branch de desenvolvimento e publicação é **master**. Versões anteriores permanecem nas tags.
Distribuições locais: `versoes/X.Y.Z/`. Binários completos são publicados nas Releases.
Componentes e licenças: [TERCEIROS.md](TERCEIROS.md).
