# Estrutura C# 14 / .NET 10

`Application/Program.cs` inicia diagnósticos ou `Desktop/DesktopApp.cs`.
`App.xaml` registra recursos e metadados WinUI; a janela é construída em C#.

| Pasta | Responsabilidade |
|---|---|
| Desktop | Layout WinUI 3, eventos de conversão, telemetria, painel WebView2 e captura de diagnóstico |
| Processing | Lote, conversão, verificação, retomada e relatórios |
| Archives | SharpCompress principal; SevenZipArchive preserva o adaptador de reserva |
| Infrastructure | Processos, JSON, HTTP limitado, contadores e ferramentas incluídas |
| Services | Consulta/cache da base e capas, sem dependência de UI |
| Media / Storage | Imagens, CUE, serial, nomes, caminhos e integridade dos arquivos |
| Configuration / Models | Configurações validadas e dados compartilhados |
| Properties | Versão, compilação e changelog visível |
| Interface / Diagnostics | Referência histórica WinForms 1.3; explicitamente excluída da compilação |

O csproj inclui apenas os módulos atuais. Os fontes históricos estão preservados,
mas não devem ser usados para novas funções. Não há dependência de Windows Forms
ou System.Web no aplicativo compilado.

Comandos: `--run-test entrada saida PS2`, `--stop-test`, `--delete-test`,
`--archive-test arquivo destino`, `--cover-test serial arquivo`, `--ui-smoke relatório`.
Use somente pastas descartáveis nos testes de exclusão.

`compilar.ps1` cria o pacote por versão. `packages.lock.json` fixa as dependências.
Atualize AppInfo.cs/AssemblyInfo.cs juntos; BuildInfo.cs é gerado ao compilar.

Projeto: DiscForge-CHD.csproj. Desktop separa Layout, Controls, Settings, Events,
Processing, Telemetry, About e Diagnostics. VisualTheme centraliza aparência;
OptionHelp centraliza explicações. Comentários descrevem responsabilidades e
invariantes das funções principais. Assets guarda o ícone em PNG e ICO.
BuildTools, na raiz do repositório, contém o iniciador portátil e seu empacotador.
