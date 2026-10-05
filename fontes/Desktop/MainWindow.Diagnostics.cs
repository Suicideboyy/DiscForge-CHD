
sealed partial class MainWindow
{
    async void RunSmoke(string destination)
    {
        try
        {
            await Task.Delay(4000);
            Report("Entrada 1 / 1: UI diagnostics");
            await Task.Delay(100);
            var facts = new GameInfo
            {
                Title = "WinUI 3 test <safe>", Serial = "SLUS-21296", Type = "CD", System = "PS2",
                CurrentFormat = "CHD v5", CurrentSize = 1048576, SourceFormat = "RAR", SourceSize = 2097152,
                Source = "PRIVATE_SOURCE_PATH", Image = "PRIVATE_IMAGE_PATH",
                DatabaseUrl = "https://private.invalid/database", CoverUrl = "https://private.invalid/cover",
                Detection = "PRIVATE_DETECTION", Lookup = "PRIVATE_LOOKUP", Detail = "PRIVATE_DETAIL"
            };
            string factsHtml = GameHtml.Render(facts, null);
            if (factsHtml.Contains("PRIVATE_") || factsHtml.Contains("private.invalid")
                || !factsHtml.Contains("&lt;safe&gt;") || !factsHtml.Contains("CHD v5")
                || !factsHtml.Contains("1.00 MiB") && !factsHtml.Contains("1,00 MiB"))
                throw new InvalidOperationException("Game facts exposed diagnostics or lost escaped values.");
            await game.UpdateAsync(facts, true);
            Report("Comprimindo: UI diagnostics");
            Report("Etapa: 42%");
            await RefreshTelemetry();
            await Task.Delay(2000);
            if (stage.Value != 42 || !clock.IsRunning || clock.Elapsed.TotalSeconds < 1)
                throw new InvalidOperationException("UI progress or timer failed.");
            Report("Lote PS2: 100% (1/1)");
            await Task.Delay(200);
            if (clock.IsRunning || batch.Value != 100)
                throw new InvalidOperationException("Batch completion did not update the UI.");
            if (helpButtons.Count < 12) throw new InvalidOperationException("Help buttons are missing.");
            SetSettingsEnabled(false);
            if (toolSettingsPage.IsEnabled)
                throw new InvalidOperationException("Tool settings remained enabled during conversion.");
            SetSettingsEnabled(true);
            int previousLanguage = language.SelectedIndex;
            language.SelectedIndex = 1;
            await Task.Delay(150);
            if (Localization.Language != "pt-BR" ||
                (string)((Microsoft.UI.Xaml.Controls.TabViewItem)tabs.TabItems[1]).Header != "Configurações")
                throw new InvalidOperationException("Portuguese UI did not load.");
            language.SelectedIndex = previousLanguage;
            await Task.Delay(150);
            tabs.SelectedIndex = 0;
            string previousInput = input.Text, previousOutput = output.Text;
            output.Text = "";
            input.Text = Path.Combine(Path.GetTempPath(), "DiscForgeSmoke");
            await Task.Delay(150);
            if (output.Text != Path.Combine(input.Text, "otimizados"))
                throw new InvalidOperationException("Default output did not follow the input folder.");
            input.Text = previousInput;
            output.Text = previousOutput;
            dvdCodecChoices[4].IsChecked = true;
            if (dvdCodecChoices[4].IsChecked == true)
                throw new InvalidOperationException("The four-codec limit was not enforced.");
            bool?[] previousCd = cdCodecChoices.ConvertAll(choice => choice.IsChecked).ToArray();
            bool? previousLegacy = legacyCompatibility.IsChecked;
            legacyCompatibility.IsChecked = false;
            foreach (var choice in cdCodecChoices) choice.IsChecked = false;
            if (CodecSelectionError() is not string codecMessage || !codecMessage.Contains("CD"))
                throw new InvalidOperationException("Missing CD codecs were not explained.");
            legacyCompatibility.IsChecked = true;
            if (cdHunk.IsEnabled || dvdHunk.IsEnabled || cdCodecChoices[0].IsEnabled ||
                dvdCodecChoices[0].IsEnabled || CodecSelectionError() != null ||
                legacyWarning.Visibility != Microsoft.UI.Xaml.Visibility.Visible)
                throw new InvalidOperationException("Legacy profile did not override modern encoding options.");
            legacyCompatibility.IsChecked = previousLegacy;
            for (int i = 0; i < cdCodecChoices.Count; i++) cdCodecChoices[i].IsChecked = previousCd[i];
            await SaveSnapshotAsync(destination + ".png");
            advanced.IsExpanded = true;
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".advanced.png");
            advanced.IsExpanded = false;
            await game.SavePreviewAsync(destination + ".game.png");
            await telemetry.SavePreviewAsync(destination + ".telemetry.png");

            var presenter = (Microsoft.UI.Windowing.OverlappedPresenter)AppWindow.Presenter;
            presenter.Maximize();
            await Task.Delay(300);
            var root = (Microsoft.UI.Xaml.FrameworkElement)Content;
            await SaveSnapshotAsync(destination + ".maximized.png");
            if (Math.Abs(root.ActualWidth - tabs.ActualWidth) > 1 ||
                Math.Abs(root.ActualHeight - tabs.ActualHeight) > 1)
                throw new InvalidOperationException($"Maximized layout: root {root.ActualWidth}×{root.ActualHeight}; tabs {tabs.ActualWidth}×{tabs.ActualHeight}.");
            presenter.Restore();

            AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 900));
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".narrow.png");
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1380, 1000));
            tabs.SelectedIndex = 2;
            aboutChanges.IsExpanded = true;
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".about.png");
            await File.WriteAllTextAsync(destination, "WinUI3=OK\nWebView2=" + game.BrowserStatus
                + "\nTelemetry=" + telemetry.Status
                + "\nMaximized layout=OK\nCHD v4 warning=OK"
                + "\nProgress/Timer=OK\nTool settings=OK\nThreads=" + threads.Value
                + "\nCPU=" + cpu.Text + "\nDisk=" + disk.Text);
        }
        catch (Exception ex) { await File.WriteAllTextAsync(destination, ex.ToString()); }
        finally { Close(); }
    }
}
