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
    bool cancelRequested;
    bool closed;
    string activity = "Waiting";

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
            catch (Exception ex) { Append("Game data: " + ex.Message); }
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
            batchStatus.Text = TranslateReport(line);
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
            activity = TranslateReport(line);
            status.Text = activity;
            stage.Value = 0;
        }
        Append(TranslateReport(line));
    }

    // Keep the engine's stable report protocol while presenting common progress in English.
    static string TranslateReport(string line)
    {
        foreach (var (source, target) in new (string, string)[]
        {
            ("Entrada ", "Input "), ("Lote ", "Batch "),
            ("Comprimindo", "Encoding"), ("Descompactando", "Extracting"),
            ("Extraindo", "Extracting"), ("Verificando", "Verifying"),
            ("ERRO:", "ERROR:"), ("Já existente:", "Already exists:")
        })
            if (line.StartsWith(source, StringComparison.OrdinalIgnoreCase))
                return target + line[source.Length..];
        return line;
    }

    // Capture validated options before locking controls and running the engine off the UI thread.
    async Task StartAsync()
    {
        try
        {
            if (!double.IsFinite(threads.Value) || !double.IsFinite(cdHunk.Value) || !double.IsFinite(dvdHunk.Value))
                throw new InvalidOperationException("Enter valid thread and hunk values.");
            var options = new EncoderSettings
            {
                Input = input.Text,
                Output = output.Text,
                Platform = (string)platform.SelectedItem,
                Threads = checked((int)threads.Value),
                CdHunk = checked((int)cdHunk.Value),
                DvdHunk = checked((int)dvdHunk.Value),
                Cd = SelectedCodecs(cdCodecChoices),
                Dvd = SelectedCodecs(dvdCodecChoices),
                ChdmanPath = chdmanPath.Text.Trim(),
                SevenZipExePath = sevenZipExePath.Text.Trim(),
                SevenZipDllPath = sevenZipDllPath.Text.Trim(),
                Online = online.IsChecked == true,
                Delete = delete.IsChecked == true
            };
            options.Validate();
            SavePreferences();
            running = true;
            stopRequested = false;
            cancelRequested = false;
            SetSettingsEnabled(false);
            stop.IsEnabled = true;
            stopNow.IsEnabled = true;
            log.Text = "";
            batch.Value = stage.Value = 0;
            clock.Reset();
            int result = await Task.Run(() => Engine.Run(options, Report));
            status.Text = result switch
            {
                0 => "Conversion complete",
                2 => cancelRequested ? "Stopped now; temporary files removed"
                    : "Stopped after the current input",
                _ => "Completed with errors; see the log"
            };
            if (result == 0)
                batch.Value = 100;
        }
        catch (Exception ex) { status.Text = "Error: " + ex.Message; Append(status.Text); }
        finally
        {
            clock.Stop();
            running = false;
            SetSettingsEnabled(true);
            stop.IsEnabled = false;
            stopNow.IsEnabled = false;
        }
    }

    void RequestStop()
    {
        stopRequested = true;
        if (Engine.StopFile != null)
            File.WriteAllText(Engine.StopFile, "");
        stop.IsEnabled = false;
        status.Text = "Stop requested; finishing the current input…";
    }

    // Cancel the current operation; the engine removes temporary files and preserves the source.
    void RequestStopNow()
    {
        if (!running) return;
        cancelRequested = true;
        Engine.CancelNow();
        stopNow.IsEnabled = stop.IsEnabled = false;
        status.Text = "Canceling and removing temporary files…";
    }
}
