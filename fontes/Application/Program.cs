using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            var inherited = Environment.GetEnvironmentVariables();
            var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in inherited.Keys)
            {
                if (canonical.ContainsKey(key))
                {
                    string value = canonical[key];
                    Environment.SetEnvironmentVariable(key, null);
                    Environment.SetEnvironmentVariable(key, value);
                }
                else
                {
                    canonical[key] = (string)inherited[key];
                }
            }

            if (args.Length == 3 && args[0] == "--cover-test")
            {
                using (var image = CoverService.Load(args[1]).GetAwaiter().GetResult())
                {
                    if (image == null)
                    {
                        return 3;
                    }

                    image.Save(args[2]);
                }

                return 0;
            }

            if (args.Length == 4 && (args[0] == "--run-test" || args[0] == "--stop-test"
                || args[0] == "--delete-test"))
            {
                var s = new EncoderSettings
                {
                    Input = args[1],
                    Output = args[2],
                    Platform = args[3],
                    Online = false,
                    Delete = args[0] == "--delete-test",
                    Threads = Math.Min(4, Environment.ProcessorCount)
                };
                var lines = new List<string>();
                int code = Engine.Run(s, delegate (string t)
                {
                    lock (lines)
                    {
                        lines.Add(t);
                    }

                    if (args[0] == "--stop-test" && t.StartsWith("Tipo:") && Engine.StopFile != null)
                    {
                        File.WriteAllText(Engine.StopFile, "");
                    }
                }

                ).GetAwaiter().GetResult();
                File.WriteAllLines(Path.Combine(s.Input, "app-test.log"), lines, Encoding.UTF8);
                return code;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if ((args.Length == 2 || args.Length == 3) && args[0] == "--ui-test")
            {
                return MainForm.SavePreview(args);
            }

            bool created;
            using (var mutex = new Mutex(true, "Local\\CHDOptimizerDesktop", out created))
            {
                if (!created)
                {
                    MessageBox.Show("O CHD Optimizer já está aberto.");
                    return 1;
                }

                Application.Run(new MainForm());
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (args.Length > 0)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "chd-optimizer-error.txt"), ex.ToString());
                return 1;
            }

            MessageBox.Show(ex.Message, "CHD Optimizer");
            return 1;
        }
    }
}
