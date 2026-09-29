# Estrutura do código

[English](../../fontes/README.md)

`fontes/DiscForge-CHD.csproj` é o projeto ativo em C# 14 e .NET 10. `Application` inicia o aplicativo; `Desktop` contém a interface WinUI 3, preferências, ajuda, indicadores e WebView2. `Processing` organiza lotes, conversão, verificação e relatórios. `Archives` usa SharpCompress e 7-Zip de reserva. `Media` lê imagens, CUE e identifica PS1/PS2. `Services` consulta jogos e capas. `Infrastructure` reúne execução de ferramentas, HTTP, logs e relatórios. `Configuration`, `Models`, `Storage` e `Properties` guardam opções, dados, segurança de arquivos e versões.

`BuildTools` cria o iniciador portátil. `fontes/build.ps1` compila a distribuição em `versions/X.Y.Z/`; `Publish.ps1` publica a versão no GitHub. Testes locais devem usar cópias dos jogos. Comandos de diagnóstico estão descritos no [arquivo em inglês](../../fontes/README.md).
