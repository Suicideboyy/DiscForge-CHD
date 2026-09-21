# DiscForge CHD

CHD Optimizer: aplicativo Windows x64 em C# para converter e otimizar imagens
de jogos PS1/PS2 em CHD, usando chdman e 7-Zip.

## Usar

Baixe [CHD-Optimizer.exe](CHD-Optimizer.exe), abra o programa e selecione as
pastas de entrada/saída, a plataforma e as opções de compressão.
Consulte [LEIA-ME.txt](LEIA-ME.txt) para detalhes e limitações.

- CD/DVD, arquivos compactados e CUE multifaixa.
- Consulta por serial e capas de jogos PS2, inclusive com mídia já identificada.
- Tempo por entrada, CPU/discos em tempo real e threads automáticas.
- Interface colorida com controles arredondados.
- Progresso, retomada e verificação antes de publicar o CHD.
- Exclusão opcional do compactado somente após sucesso integral.
- O aplicativo não depende de scripts BAT ou PowerShell para processar jogos.

## Desenvolver e publicar

Código completo em [fontes](fontes). O chdman e o 7-Zip incluídos pertencem
aos respectivos autores. O repositório não contém jogos nem imagens de discos.

Após alterar, testar e incrementar a versão:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Publicar.ps1
```

O comando compila e envia fonte, executável e tag para o GitHub configurado
como `origin`. Não utiliza consultas periódicas a uma IA.
Veja [PUBLICACAO.md](PUBLICACAO.md), [CHANGELOG.txt](CHANGELOG.txt) e
[TESTES.txt](TESTES.txt).
