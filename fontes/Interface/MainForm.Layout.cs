using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

partial class MainForm : Form
{
    TabControl tabs = new TabControl();
    PictureBox cover = new PictureBox();
    Label coverStatus = new Label();
    TextBox gameInfo = new TextBox();
    int coverGeneration;
    string coverSerial = "";
    TextBox input = new TextBox();
    TextBox output = new TextBox();
    TextBox log = new TextBox();
    ComboBox platform = new ComboBox();
    ComboBox cdh = new ComboBox();
    ComboBox dvdh = new ComboBox();
    CheckedListBox cd = new CheckedListBox();
    CheckedListBox dvd = new CheckedListBox();
    NumericUpDown threads = new NumericUpDown();
    CheckBox delete = new CheckBox();
    CheckBox online = new CheckBox();
    Button start = new Button();
    Button stop = new Button();
    ProgressBar batch = new ProgressBar();
    ProgressBar stage = new ProgressBar();
    Label batchText = new Label();
    Label stageText = new Label();
    Label hint = new Label();
    TableLayoutPanel options;
    bool running;
    string activity = "Aguardando";
    public MainForm()
    {
        Text = "CHD Optimizer 1.2.1 • PS1 e PS2";
        Font = new Font("Segoe UI", 10);
        ClientSize = new Size(1190, 780);
        MinimumSize = new Size(1110, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(246, 248, 251);
        tabs.Dock = DockStyle.Fill;
        Controls.Add(tabs);
        var conversion = new TabPage("Conversão");
        tabs.TabPages.Add(conversion);
        var columns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285));
        conversion.Controls.Add(columns);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 9
        };
        columns.Controls.Add(layout, 0, 0);
        var game = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 20, 12, 12),
            RowCount = 4,
            ColumnCount = 1
        };
        game.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        game.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        game.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        game.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        columns.Controls.Add(game, 1, 0);
        game.Controls.Add(new Label
        {
            Text = "Jogo atual",
            AutoSize = true,
            Font = new Font("Segoe UI", 15, FontStyle.Bold)
        });
        cover.Dock = DockStyle.Fill;
        cover.SizeMode = PictureBoxSizeMode.Zoom;
        cover.BackColor = Color.WhiteSmoke;
        game.Controls.Add(cover);
        coverStatus.Dock = DockStyle.Fill;
        coverStatus.Text = "Aguardando identificação";
        game.Controls.Add(coverStatus);
        gameInfo.Multiline = true;
        gameInfo.ReadOnly = true;
        gameInfo.Dock = DockStyle.Fill;
        gameInfo.ScrollBars = ScrollBars.Vertical;
        gameInfo.BackColor = Color.White;
        gameInfo.Text = "Os dados consultados aparecerão aqui.";
        game.Controls.Add(gameInfo);
        BuildAbout();
        foreach (int h in new[]{48, 40, 40, 250, 28, 48, 48, 48})
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        }

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Text = "CHD Optimizer",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            AutoSize = true
        });
        layout.Controls.Add(PathRow("Entrada", input));
        layout.Controls.Add(PathRow("Saída", output));
        options = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 5
        };
        layout.Controls.Add(options);
        foreach (int w in new[]{140, 270, 145, 260})
        {
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, w));
        }

        options.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        options.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        options.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        options.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        platform.DropDownStyle = ComboBoxStyle.DropDownList;
        platform.Items.AddRange(new object[]{"PS1", "PS2"});
        platform.SelectedIndex = 1;
        threads.Minimum = 1;
        threads.Maximum = Environment.ProcessorCount;
        threads.Value = Math.Max(1, Environment.ProcessorCount - 2);
        AddOption("Plataforma", platform, 0, 0);
        AddOption("Threads", threads, 2, 0);
        cdh.DropDownStyle = dvdh.DropDownStyle = ComboBoxStyle.DropDownList;
        cdh.Items.AddRange(new object[]{"2448", "19584", "78336", "1047744"});
        dvdh.Items.AddRange(new object[]{"2048", "4096", "32768", "262144", "1048576"});
        cdh.SelectedIndex = dvdh.SelectedIndex = 0;
        AddOption("Hunk CD (bytes)", cdh, 0, 1);
        AddOption("Hunk DVD (bytes)", dvdh, 2, 1);
        cd.CheckOnClick = dvd.CheckOnClick = true;
        foreach (string c in new[]{"cdlz", "cdzs", "cdzl", "cdfl"})
        {
            cd.Items.Add(c, true);
        }

        foreach (string c in new[]{"lzma", "zstd", "zlib", "flac", "huff"})
        {
            dvd.Items.Add(c, c != "huff");
        }

        AddOption("Codecs de CD", cd, 0, 2);
        AddOption("Codecs de DVD", dvd, 2, 2);
        online.Text = "Consultar serial e nome (PS2)";
        online.Checked = true;
        online.AutoSize = true;
        delete.Text = "Apagar compactado após sucesso (PS2)";
        delete.AutoSize = true;
        delete.Checked = true;
        options.Controls.Add(online, 0, 3);
        options.SetColumnSpan(online, 2);
        options.Controls.Add(delete, 2, 3);
        options.SetColumnSpan(delete, 2);
        hint.AutoSize = true;
        hint.Text = "PS2: compactados, ISO, BIN/CUE e CHD. Até 4 codecs por mídia.";
        layout.Controls.Add(hint);
        layout.Controls.Add(ProgressRow(batchText, batch, "Lote: 0%"));
        layout.Controls.Add(ProgressRow(stageText, stage, "Etapa: aguardando"));
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill
        };
        start.Text = "Iniciar";
        start.Width = 130;
        start.Height = 34;
        stop.Text = "Parar após atual";
        stop.Width = 170;
        stop.Height = 34;
        stop.Enabled = false;
        var open = new Button
        {
            Text = "Abrir saída",
            Width = 120,
            Height = 34
        };
        open.Click += delegate
        {
            if (Directory.Exists(output.Text))
            {
                Process.Start("explorer.exe", output.Text);
            }
        }

        ;
        actions.Controls.AddRange(new Control[]{start, stop, open});
        layout.Controls.Add(actions);
        log.Multiline = true;
        log.ReadOnly = true;
        log.ScrollBars = ScrollBars.Vertical;
        log.Dock = DockStyle.Fill;
        log.BackColor = Color.White;
        layout.Controls.Add(log);
        start.Click += async delegate
        {
            await Start();
        }

        ;
        stop.Click += delegate
        {
            if (Engine.StopFile != null)
            {
                File.WriteAllText(Engine.StopFile, "");
                stop.Enabled = false;
                Append("Parada solicitada: a entrada atual será concluída antes de parar.");
            }
        }

        ;
        platform.SelectedIndexChanged += delegate
        {
            bool ps2 = platform.Text == "PS2";
            dvd.Enabled = dvdh.Enabled = online.Enabled = delete.Enabled = ps2;
            hint.Text = ps2 ? "PS2: compactados, ISO, BIN/CUE e CHD. Até 4 codecs por mídia."
                : "PS1: otimiza arquivos CHD na pasta de entrada, conforme o BAT original.";
        }

        ;
        FormClosing += delegate (object sender, FormClosingEventArgs e)
        {
            if (running)
            {
                e.Cancel = true;
                MessageBox.Show(this, "Use ‘Parar após atual’ e aguarde a conclusão para fechar.");
            }
        }

        ;
    }
}
