using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;

partial class MainForm
{
    void Append(string text)
    {
        if (log.TextLength > 100000)
        {
            log.Text = log.Text.Substring(log.TextLength - 70000);
        }

        log.AppendText(text + Environment.NewLine);
    }

    void Report(string line)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(new Action(delegate
        {
            if (line.StartsWith("GUI_GAME:"))
            {
                try
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(9)));
                    UpdateGame(new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json));
                }
                catch
                {
                    Append("Não foi possível exibir os dados desta entrada.");
                }

                return;
            }

            var total = Regex.Match(line, @"Lote PS[12]:\s*([\d,.]+)%.*\((\d+)/(\d+)");
            var ps1 = Regex.Match(line, @"GUI_ITEM=(\d+)/(\d+)");
            if (total.Success)
            {
                batch.Value = Math.Min(100, (int)(100.0 * Int32.Parse(total.Groups[2].Value) / Math.Max(1,
                    Int32.Parse(total.Groups[3].Value))));
                batchText.Text = line;
                return;
            }

            if (ps1.Success)
            {
                batch.Value = Math.Min(100, (int)(100.0 * Int32.Parse(ps1.Groups[1].Value) / Math.Max(1,
                    Int32.Parse(ps1.Groups[2].Value))));
                batchText.Text = "Lote: " + batch.Value + "%";
                return;
            }

            var progress = Regex.Match(line,
                @"(?:Etapa:\s*|(?:Compressing|Extracting|Verifying),\s*)(\d+(?:[.,]\d+)?)%");
            if (progress.Success)
            {
                double p = Double.Parse(progress.Groups[1].Value.Replace(',', '.'),
                    System.Globalization.CultureInfo.InvariantCulture);
                stage.Value = Math.Max(0, Math.Min(100, (int)p));
                stageText.Text = activity + " — " + p.ToString("N1") + "%";
                return;
            }

            if (line.StartsWith("Comprimindo") || line.StartsWith("Descompactando")
                || line.StartsWith("Extraindo") || line.StartsWith("Verificando"))
            {
                activity = line;
                stage.Value = 0;
                stageText.Text = line;
            }

            Append(line);
        }

        ));
    }

    async Task Start()
    {
        try
        {
            var s = new EncoderSettings
            {
                Input = input.Text,
                Output = output.Text,
                Platform = platform.Text,
                Cd = Codecs(cd),
                Dvd = Codecs(dvd),
                CdHunk = Int32.Parse(cdh.Text),
                DvdHunk = Int32.Parse(dvdh.Text),
                Threads = (int)threads.Value,
                Delete = delete.Checked,
                Online = online.Checked
            };
            s.Validate();
            running = true;
            options.Enabled = input.Enabled = output.Enabled = start.Enabled = false;
            stop.Enabled = true;
            batch.Value = stage.Value = 0;
            log.Clear();
            Append("Preparando ferramentas incluídas…");
            int code = await Task.Run(() => Engine.Run(s, Report));
            if (code == 0)
            {
                batch.Value = 100;
                batchText.Text = "Lote: 100% concluído";
                Append("Concluído. Consulte os detalhes acima e o log em " + Path.Combine(s.Input, "temp",
                    "log.txt"));
            }
            else
            {
                Append(code == 2 ? "Interrompido após concluir a entrada atual."
                    : "Execução terminou com erros. Consulte o log acima.");
            }
        }
        catch (Exception ex)
        {
            Append("ERRO: " + ex.Message);
            MessageBox.Show(this, ex.Message, "CHD Optimizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            running = false;
            options.Enabled = input.Enabled = output.Enabled = start.Enabled = true;
            stop.Enabled = false;
        }
    }
}
