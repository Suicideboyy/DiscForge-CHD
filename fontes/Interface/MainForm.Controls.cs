using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

partial class MainForm
{
    Control PathRow(string title, TextBox box)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        row.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Padding = new Padding(0, 5, 0, 0)
        });
        box.Dock = DockStyle.Fill;
        row.Controls.Add(box);
        var b = new RoundedButton
        {
            Text = "Escolher…",
            Dock = DockStyle.Fill
        };
        b.Click += delegate
        {
            if (running)
            {
                return;
            }

            using (var dialog = new FolderBrowserDialog
            {
                Description = "Selecione a pasta de " + title.ToLower(),
                SelectedPath = box.Text
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    box.Text = dialog.SelectedPath;
                    if (box == input && output.Text.Length == 0)
                    {
                        output.Text = Path.Combine(box.Text, "otimizados");
                    }
                }
            }
        }

        ;
        row.Controls.Add(b);
        return row;
    }

    void AddOption(string title, Control control, int x, int y)
    {
        options.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 0)
        }, x, y);
        control.Dock = DockStyle.Fill;
        options.Controls.Add(control, x + 1, y);
    }

    Control ProgressRow(Label text, RoundedProgress bar, string initial)
    {
        var p = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2
        };
        text.Text = initial;
        text.AutoSize = true;
        bar.Dock = DockStyle.Fill;
        p.Controls.Add(text);
        p.Controls.Add(bar);
        return p;
    }

    static string Codecs(CheckedListBox list)
    {
        var names = new List<string>();
        foreach (var item in list.CheckedItems)
        {
            names.Add(item.ToString());
        }

        return String.Join(",", names);
    }
}
