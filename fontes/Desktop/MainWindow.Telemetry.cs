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

    // Evita amostragens sobrepostas; a coleta dos contadores não bloqueia a janela.
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

}
