using System;
using System.Drawing;
using System.Windows.Forms;

partial class MainForm
{
    void BuildAbout()
    {
        var page = new TabPage("Sobre");
        tabs.TabPages.Add(page);
        var sub = new TabControl
        {
            Dock = DockStyle.Fill
        };
        page.Controls.Add(sub);
        var info = new TabPage("Informações");
        var changes = new TabPage("Changelog");
        sub.TabPages.Add(info);
        sub.TabPages.Add(changes);
        info.Controls.Add(new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 12),
            Text = "CHD Optimizer 1.2.1\r\n\r\nCompilação do aplicativo: " + BuildInfo.Date
                + ("\r\nArquitetura: Windows 64 bits\r\nLinguagem da interface: C# / "
                + "Windows Forms / .NET Framework\r\nMotor: C# nativo / .NET "
                + "Framework\r\n\r\nCHDman incluído: MAME 0.289 (unknown)\r\nCompilação do "
                + "CHDman: 14/09/2026\r\nLinguagem do CHDman: C++20\r\nGCC 16.2 / Zen 3 / "
                + "LTO / LZMA aprimorado\r\nA data e versão acima referem-se ao binário "
                + "incluído; não houve recompilação do CHDman nesta "
                + "atualização.\r\n\r\nCapas PS2: xlenore/ps2-covers (GitHub)\r\nCache: "
                + "pasta covers em %LOCALAPPDATA%\\CHD Optimizer\r\nA falta de capa ou de "
                + "conexão não interrompe o processamento.\r\n\r\nComponentes: chdman/MAME "
                + "e 7-Zip, dos respectivos autores.")
        });
        changes.Controls.Add(new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 12),
            Text = ("1.2.1 — 21/09/2026\r\n\r\n• Código organizado por responsabilidade, com "
                + "blocos e linhas menores.\r\n• Interface, arquivos, ferramentas e "
                + "consultas em módulos separados.\r\n• Regras de conversão "
                + "preservadas.\r\n\r\n1.2.0 — 20/09/2026\r\n\r\n• Motor convertido "
                + "integralmente para C#; sem BAT, CMD ou PowerShell durante a "
                + "conversão.\r\n• Execução direta do chdman e 7-Zip, verificação antes "
                + "de publicar e remoção segura do compactado.\r\n• Preservados painel, "
                + "capas, consultas, retomada, codecs e seleção de " + "plataforma.\r\n\r\n1.1.0 — ")
                + BuildInfo.Date + ("\r\n\r\n• Painel do jogo: título, serial, mídia, identificação, "
                + "consulta, origem e status.\r\n• Download de capas PS2 por serial, com "
                + "cache e atualização sem bloquear a interface.\r\n• BINs numerados "
                + "como Track são faixas do mesmo disco, não discos distintos.\r\n• "
                + "Faixas sem CUE são bloqueadas com uma mensagem clara; compactados "
                + "são preservados.\r\n• CHD parcial antigo não autoriza ignorar um "
                + "conjunto de faixas sem CUE.\r\n• Aba Sobre com versões, datas, "
                + "linguagens e este changelog.\r\n\r\n1.0.0 — 20/09/2026\r\n\r\n• Aplicativo "
                + "portátil com BAT, chdman e 7-Zip incluídos.\r\n• Seleção de pastas, "
                + "plataforma, codecs, hunk e threads.\r\n• Progresso por lote e etapa; "
                + "retomada e parada após a entrada atual.")
        });
    }
}
