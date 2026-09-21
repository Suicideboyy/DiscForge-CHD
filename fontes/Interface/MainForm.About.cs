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
            Text = AppInfo.DisplayName + "\r\n\r\nCompilação do aplicativo: " + BuildInfo.Date
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
            Text = AppChangelog.Text
        });
    }
}
