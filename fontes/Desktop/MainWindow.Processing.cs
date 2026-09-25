using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;

sealed partial class MainWindow
{
    bool running;
    volatile bool stopRequested;
    bool closed;
    string activity = "Aguardando";

    void Append(string text)
    {
        log.Text += text + Environment.NewLine;
        if (log.Text.Length > 50000)
            log.Text = log.Text[^40000..];
        log.SelectionStart = log.Text.Length;
    }

    void Report(string line)
    {
        if (stopRequested && Engine.StopFile != null)
            File.WriteAllText(Engine.StopFile, "");
        DispatcherQueue.TryEnqueue(() => ApplyReport(line));
    }

    void ApplyReport(string line)
    {
        if (closed)
            return;
        if (line.StartsWith("GUI_GAME:"))
        {
            try
            {
                var info = JsonData.Read<GameInfo>(Encoding.UTF8.GetString(Convert.FromBase64String(line[9..])));
                _ = game.UpdateAsync(info, (string)platform.SelectedItem == "PS2");
            }
            catch (Exception ex) { Append("Dados do jogo: " + ex.Message); }
            return;
        }
        if (Regex.IsMatch(line, @"^Entrada \d+ / \d+:"))
        {
            clock.Restart();
            game.Reset();
        }
        var total = Regex.Match(line, @"^Lote .*?: ([\d.,]+)% \((\d+)/(\d+)\)");
        if (total.Success)
        {
            clock.Stop();
            batch.Value = 100.0 * int.Parse(total.Groups[2].Value) / Math.Max(1, int.Parse(total.Groups[3].Value));
            batchStatus.Text = line;
        }
        var progress = Regex.Match(line, @"^Etapa:\s*(\d+(?:[.,]\d+)?)%");
        if (progress.Success)
        {
            stage.Value = Math.Clamp(double.Parse(progress.Groups[1].Value.Replace(',', '.'),
                CultureInfo.InvariantCulture), 0, 100);
            status.Text = activity + " — " + stage.Value.ToString("N1") + "%";
            return;
        }
        if (line.StartsWith("GUI_"))
            return;
        if (line.StartsWith("Comprimindo") || line.StartsWith("Descompactando")
            || line.StartsWith("Verificando") || line.StartsWith("Extraindo"))
        {
            activity = line;
            status.Text = line;
            stage.Value = 0;
        }
        Append(line);
    }

    async Task StartAsync()
    {
        try
        {
            if (!double.IsFinite(threads.Value) || !double.IsFinite(cdHunk.Value) || !double.IsFinite(dvdHunk.Value))
                throw new InvalidOperationException("Preencha threads e hunks com números válidos.");
            var options = new EncoderSettings
            {
                Input = input.Text,
                Output = output.Text,
                Platform = (string)platform.SelectedItem,
                Threads = checked((int)threads.Value),
                CdHunk = checked((int)cdHunk.Value),
                DvdHunk = checked((int)dvdHunk.Value),
                Cd = cdCodecs.Text.Trim(),
                Dvd = dvdCodecs.Text.Trim(),
                Online = online.IsChecked == true,
                Delete = delete.IsChecked == true
            };
            options.Validate();
            running = true;
            stopRequested = false;
            SetSettingsEnabled(false);
            stop.IsEnabled = true;
            log.Text = "";
            batch.Value = stage.Value = 0;
            clock.Reset();
            int result = await Task.Run(() => Engine.Run(options, Report));
            status.Text = result switch
            {
                0 => "Conversão concluída",
                2 => "Parado após a entrada atual",
                _ => "Concluído com erros; consulte o log"
            };
            if (result == 0)
                batch.Value = 100;
        }
        catch (Exception ex) { status.Text = "Erro: " + ex.Message; Append(status.Text); }
        finally
        {
            clock.Stop();
            running = false;
            SetSettingsEnabled(true);
            stop.IsEnabled = false;
        }
    }

    void RequestStop()
    {
        stopRequested = true;
        if (Engine.StopFile != null)
            File.WriteAllText(Engine.StopFile, "");
        stop.IsEnabled = false;
        status.Text = "Parada solicitada; finalizando a entrada atual…";
    }
}
