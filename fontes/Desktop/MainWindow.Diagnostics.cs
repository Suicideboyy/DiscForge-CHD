using System;
using System.IO;
using System.Threading.Tasks;

sealed partial class MainWindow
{
    async void RunSmoke(string destination)
    {
        try
        {
            await Task.Delay(4000);
            Report("Entrada 1 / 1: UI diagnostics");
            await Task.Delay(100);
            await game.UpdateAsync(new GameInfo { Title = "WinUI 3 test", Serial = "SLUS-21296", Type = "CD" }, true);
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
            if (helpButtons.Count < 13) throw new InvalidOperationException("Help buttons are missing.");
            const ulong gib = 1073741824;
            if (MemoryStatus.IsEligible(12 * gib, 5 * gib) ||
                MemoryStatus.IsEligible(16 * gib, 5 * gib - 1) ||
                !MemoryStatus.IsEligible(16 * gib, 5 * gib))
                throw new InvalidOperationException("RAM thresholds failed.");
            string previousRamPath = ramDiskPath.Text;
            bool? previousRamChoice = ramExtraction.IsChecked;
            ramDiskPath.Text = Path.GetTempPath();
            RefreshRamAvailability();
            if (ramExtraction.IsEnabled)
                throw new InvalidOperationException("RAM extraction accepted an ordinary disk.");
            SetSettingsEnabled(false);
            if (ramDiskPath.IsEnabled || ramExtraction.IsEnabled || toolSettingsPage.IsEnabled)
                throw new InvalidOperationException("RAM settings remained enabled during conversion.");
            SetSettingsEnabled(true);
            ramDiskPath.Text = previousRamPath;
            ramExtraction.IsChecked = previousRamChoice;
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
            await SaveSnapshotAsync(destination + ".png");
            advanced.IsExpanded = true;
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".advanced.png");
            advanced.IsExpanded = false;
            await game.SavePreviewAsync(destination + ".game.png");

            AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 900));
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".narrow.png");
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1380, 1000));
            tabs.SelectedIndex = 2;
            aboutChanges.IsExpanded = true;
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".about.png");
            await File.WriteAllTextAsync(destination, "WinUI3=OK\nWebView2=" + game.BrowserStatus
                + "\nProgress/Timer=OK\nRAM eligibility/settings=OK\nThreads=" + threads.Value
                + "\nCPU=" + cpu.Text + "\nDisk=" + disk.Text);
        }
        catch (Exception ex) { await File.WriteAllTextAsync(destination, ex.ToString()); }
        finally { Close(); }
    }
}
