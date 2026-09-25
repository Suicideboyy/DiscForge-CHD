using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

sealed partial class MainWindow
{
    readonly Stopwatch clock = new();
    readonly SystemPerformance performance = new();
    readonly DispatcherTimer timer = new();
    bool sampling;

    async Task RefreshTelemetry()
    {
        elapsed.Text = $"{(int)clock.Elapsed.TotalHours:00}:{clock.Elapsed.Minutes:00}:{clock.Elapsed.Seconds:00}";
        if (sampling || closed) return;
        sampling = true;
        try
        {
            var sample = await Task.Run(performance.Read);
            if (closed) return;
            cpu.Text = sample.CpuPercent.HasValue ? $"{sample.CpuPercent:N1}%" : "N/D";
            disk.Text = $"Leitura: {Rate(sample.ReadBytesPerSecond)}\nEscrita: {Rate(sample.WriteBytesPerSecond)}"
                + (sample.DiskBusyPercent.HasValue ? $"\nAtividade: {sample.DiskBusyPercent:N1}%" : "\nAtividade: N/D");
            if (running) await game.RetryAsync();
        }
        catch (Exception ex) { if (!closed) disk.Text = "Métricas indisponíveis: " + ex.Message; }
        finally { sampling = false; }
    }

    static string Rate(double? bytes) => bytes.HasValue ? $"{bytes.Value / 1048576:N1} MiB/s" : "N/D";

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
            await SaveSnapshotAsync(destination + ".png");
            await game.SavePreviewAsync(destination + ".game.png");
            var tabs = (Microsoft.UI.Xaml.Controls.TabView)Content;
            tabs.SelectedIndex = 1;
            var about = (Microsoft.UI.Xaml.Controls.TabViewItem)tabs.TabItems[1];
            var scroller = (Microsoft.UI.Xaml.Controls.ScrollViewer)about.Content;
            var body = (Microsoft.UI.Xaml.Controls.StackPanel)scroller.Content;
            ((Microsoft.UI.Xaml.Controls.Expander)body.Children[2]).IsExpanded = true;
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".about.png");
            await File.WriteAllTextAsync(destination, "WinUI3=OK\nWebView2=" + game.BrowserStatus
                + "\nProgresso/Cronometro=OK\nThreads=" + threads.Value + "\nCPU=" + cpu.Text + "\nDiscos=" + disk.Text);
        }
        catch (Exception ex) { await File.WriteAllTextAsync(destination, ex.ToString()); }
        finally { Close(); }
    }
}
