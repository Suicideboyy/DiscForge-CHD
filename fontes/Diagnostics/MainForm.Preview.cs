using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

partial class MainForm
{
    internal static int SavePreview(string[] args)
    {
        using (var f = new MainForm())
        {
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(-10000, -10000);
            f.ShowInTaskbar = false;
            f.Show();
            Application.DoEvents();
            if (args.Length == 3)
            {
                if (args[2] == "game")
                {
                    f.UpdateGame(new Dictionary<string, object>{{"Title", "Dance Factory"}, {"Serial",
                        "SLUS-21296"}, {"Type", "CD"}, {"Status", "CUE AUSENTE"}, {"Detection",
                        "Seis faixas BIN; sem descritor CUE"}, {"Source", "Dance Factory (USA).7z"},
                        {"Detail", "Prévia do painel — CUE original necessário."}});
                    var watch = Stopwatch.StartNew();
                    while (f.coverStatus.Text.StartsWith("Carregando") && watch.ElapsedMilliseconds < 15000)
                    {
                        Application.DoEvents();
                        Thread.Sleep(30);
                    }
                }
                else
                {
                    f.tabs.SelectedIndex = 1;
                    if (args[2] == "changelog")
                    {
                        ((TabControl)f.tabs.TabPages[1].Controls[0]).SelectedIndex = 1;
                    }
                }

                Application.DoEvents();
            }

            var samplingWatch = Stopwatch.StartNew();
            while (samplingWatch.ElapsedMilliseconds < 2600)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }

            using (var bitmap = new Bitmap(f.Width, f.Height))
            {
                f.DrawToBitmap(bitmap, new Rectangle(0, 0, f.Width, f.Height));
                bitmap.Save(args[1]);
            }

            f.Hide();
        }

        return 0;
    }
}
