using System.Drawing;
using System.Windows.Forms;

partial class MainForm
{
    void ApplyAppearance(TabPage page, TableLayoutPanel columns,
        TableLayoutPanel layout, TableLayoutPanel game)
    {
        page.BackColor = Theme.Background;
        columns.Padding = new Padding(12);
        columns.ColumnStyles[1].Width = 300;
        layout.BackColor = Color.White;
        layout.Padding = new Padding(16);
        game.BackColor = Color.White;
        game.Margin = new Padding(10, 3, 3, 3);
        layout.SizeChanged += delegate { Theme.Round(layout, 18); };
        game.SizeChanged += delegate { Theme.Round(game, 18); };
        cover.SizeChanged += delegate { Theme.Round(cover, 12); };
        Theme.Round(layout, 18);
        Theme.Round(game, 18);
        start.BackColor = Theme.Purple;
        stop.BackColor = Theme.Teal;
        start.Text = "Iniciar conversão";
        start.Width = 160;
        stop.Width = 160;
        gameInfo.BorderStyle = BorderStyle.None;
        log.BorderStyle = BorderStyle.None;
        gameInfo.Font = new Font("Segoe UI", 9);
        log.Font = new Font("Segoe UI", 9);
        hint.ForeColor = Theme.Muted;
        hint.Font = new Font("Segoe UI", 9);
        coverStatus.Font = new Font("Segoe UI", 8);
        coverStatus.ForeColor = Theme.Muted;
        cover.BackColor = Theme.Background;
        layout.Controls[0].ForeColor = Theme.Purple;
        Control title = layout.Controls[0];
        layout.Controls.Remove(title);
        var header = new RoundedPanel
        {
            Dock = DockStyle.Fill, BackColor = Theme.Navy, Padding = new Padding(14, 3, 14, 3)
        };
        title.ForeColor = Color.White;
        title.AutoSize = false;
        title.Dock = DockStyle.Fill;
        header.Controls.Add(title);
        layout.Controls.Add(header, 0, 0);
        game.Controls[0].ForeColor = Theme.Teal;
        Theme.StyleInputs(this);
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.ItemSize = new Size(145, 34);
        tabs.DrawItem += delegate(object sender, DrawItemEventArgs args)
        {
            bool selected = args.Index == tabs.SelectedIndex;
            using (var brush = new SolidBrush(selected ? Theme.Purple : Theme.Background))
            {
                args.Graphics.FillRectangle(brush, args.Bounds);
            }
            TextRenderer.DrawText(args.Graphics, tabs.TabPages[args.Index].Text, Font, args.Bounds,
                selected ? Color.White : Theme.Ink,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
    }
}
