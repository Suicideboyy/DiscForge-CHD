
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
                bool ps2 = info.System == "PS2" ||
                    (info.System != "PS1" && (string)platform.SelectedItem == "PS2");
                _ = game.UpdateAsync(info, ps2);
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
                return Localization.T(target) + line[source.Length..];
        return line;
    }

    // Capture validated options before locking controls and running the engine off the UI thread.
    async Task StartAsync()
    {
        try
        {
            string codecError = CodecSelectionError();
            if (codecError != null)
            {
                status.Text = codecError;
                await new ContentDialog
                {
                    XamlRoot = tabs.XamlRoot,
                    Title = Localization.IsPortuguese ? "Selecione os codecs" : "Select codecs",
                    Content = codecError,
                    CloseButtonText = "OK"
                }.ShowAsync();
                return;
            }
            bool legacy = legacyCompatibility.IsChecked == true;
            if (!double.IsFinite(threads.Value) || (!legacy && (!double.IsFinite(cdHunk.Value) || !double.IsFinite(dvdHunk.Value))))
                throw new InvalidOperationException("Enter valid thread and hunk values.");
            var options = new EncoderSettings
            {
                Input = input.Text,
                Output = output.Text,
                Platform = (string)platform.SelectedItem,
                Threads = checked((int)threads.Value),
                CdHunk = legacy ? 9792 : checked((int)cdHunk.Value),
                DvdHunk = legacy ? 2048 : checked((int)dvdHunk.Value),
                Cd = SelectedCodecs(cdCodecChoices),
                Dvd = SelectedCodecs(dvdCodecChoices),
                ChdmanPath = chdmanPath.Text.Trim(),
                Online = online.IsChecked == true,
                Delete = delete.IsChecked == true,
                AutoDetectSystem = autoDetect.IsChecked == true,
                LegacyCompatibility = legacy
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
                0 => Localization.T("Conversion complete"),
                2 => cancelRequested ? Localization.T("Stopped now; temporary files removed")
                    : Localization.T("Stopped after the current input"),
                _ => Localization.T("Completed with errors; see the log")
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

    /// <summary>Explain missing codecs before starting extraction or encoding.</summary>
    string CodecSelectionError()
    {
        if (legacyCompatibility.IsChecked == true) return null;
        bool needsDvd = autoDetect.IsChecked == true || (string)platform.SelectedItem == "PS2";
        bool missingCd = SelectedCodecs(cdCodecChoices).Length == 0;
        bool missingDvd = needsDvd && SelectedCodecs(dvdCodecChoices).Length == 0;
        if (!missingCd && !missingDvd) return null;
        string media = missingCd && missingDvd ? "CD / DVD" : missingCd ? "CD" : "DVD";
        return Localization.IsPortuguese
            ? $"Selecione pelo menos um codec de {media} em Opções de codificação. Cada codec define como os dados são comprimidos."
            : $"Select at least one {media} codec in Encoding options. Each codec defines how the data is compressed.";
    }

    void RequestStop()
    {
        stopRequested = true;
        if (Engine.StopFile != null)
            File.WriteAllText(Engine.StopFile, "");
        stop.IsEnabled = false;
        status.Text = Localization.T("Stop requested; finishing the current input…");
    }

    // Cancel the current operation; the engine removes temporary files and preserves the source.
    void RequestStopNow()
    {
        if (!running) return;
        cancelRequested = true;
        Engine.CancelNow();
        stopNow.IsEnabled = stop.IsEnabled = false;
        status.Text = Localization.T("Canceling and removing temporary files…");
    }
}
