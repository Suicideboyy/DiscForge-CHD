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
            Report("Entrada 1 / 1: diagnóstico da interface");
            await Task.Delay(100);
            await game.UpdateAsync(new GameInfo { Title = "Teste WinUI 3", Serial = "SLUS-21296", Type = "CD" }, true);
            Report("Comprimindo: diagnóstico");
            Report("Etapa: 42%");
            await RefreshTelemetry();
            await Task.Delay(2000);
            if (stage.Value != 42 || !clock.IsRunning || clock.Elapsed.TotalSeconds < 1)
                throw new InvalidOperationException("Progresso ou cronômetro da interface falhou.");
            Report("Lote PS2: 100% (1/1)");
            await Task.Delay(200);
            if (clock.IsRunning || batch.Value != 100)
                throw new InvalidOperationException("Conclusão do lote não atualizou a interface.");
            if (helpButtons.Count < 10) throw new InvalidOperationException("Ajuda incompleta.");
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
            tabs.SelectedIndex = 1;
            aboutChanges.IsExpanded = true;
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".about.png");
            await File.WriteAllTextAsync(destination, "WinUI3=OK\nWebView2=" + game.BrowserStatus
                + "\nProgresso/Cronometro=OK\nThreads=" + threads.Value + "\nCPU=" + cpu.Text + "\nDiscos=" + disk.Text);
        }
        catch (Exception ex) { await File.WriteAllTextAsync(destination, ex.ToString()); }
        finally { Close(); }
    }
}
