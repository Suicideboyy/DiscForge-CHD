# Estrutura do código

Comece por `Application/Program.cs` e siga para `Processing/Engine.cs`.
A interface coleta as opções; o motor abre uma sessão e processa cada entrada.

| Pasta | Responsabilidade |
| --- | --- |
| Application | Inicialização, instância única e comandos de diagnóstico. |
| Configuration | Opções de encoder e validação das pastas e codecs. |
| Interface | Janela, controles, progresso, dados do jogo e aba Sobre. |
| Processing | Coordenação do lote, conversão, retomada e relatórios. |
| Archives | Listagem, prévia de CUE e identificação dos volumes compactados. |
| Media | Seleção das imagens, leitura de CUE, serial e nomes de saída. |
| Storage | Validação de caminhos, limpeza e conferência dos arquivos originais. |
| Infrastructure | Extração das ferramentas incluídas e execução de processos. |
| Services | Consulta/cache da base de jogos e download/cache de capas. |
| Models | Dados compartilhados entre os módulos. |
| Properties | Versão do aplicativo e data da compilação. |
| Diagnostics | Captura da interface usada nos testes. |
| tools | Binários originais de chdman e 7-Zip. |

`MainForm` é parcial para separar a construção da janela de seus eventos.
`ConversionSession` também é parcial: compartilha o estado do lote, mas mantém
conversão, retomada e relatórios em arquivos distintos. Arquivos, processos,
compactados e consultas são classes independentes, usadas pela sessão.

`compilar.ps1` encontra os fontes C# recursivamente. Novos módulos entram na
compilação sem precisar alterar uma lista fixa. Não é necessário instalar
formatadores ou bibliotecas adicionais para compilar o aplicativo.

Ao atualizar a versão, edite `Properties/AssemblyInfo.cs`, os textos visíveis
e o changelog. `Properties/BuildInfo.cs` é gerado durante a compilação.

Novos módulos: Interface/Theme.cs e MainForm.Appearance.cs definem o visual;
MainForm.Telemetry.cs apresenta tempo e métricas. Infrastructure/MachineInfo.cs
detecta processadores lógicos; SystemPerformance.cs lê os contadores do Windows.
Properties/AppInfo.cs centraliza a versão visível; AppChangelog.cs contém
somente o histórico de funções e correções mostrado ao usuário.
