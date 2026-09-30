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

    // Avoid overlapping samples; counter collection stays off the UI thread.
    async Task RefreshTelemetry()
    {
        RefreshRamAvailability();
        elapsed.Text = $"{(int)clock.Elapsed.TotalHours:00}:{clock.Elapsed.Minutes:00}:{clock.Elapsed.Seconds:00}";
        if (sampling || closed) return;
        sampling = true;
        try
        {
            var sample = await Task.Run(performance.Read);
            if (closed) return;
            cpu.Text = sample.CpuPercent.HasValue ? $"{sample.CpuPercent:N1}%" : "N/A";
            disk.Text = $"{Localization.T("Read: ")}{Rate(sample.ReadBytesPerSecond)}\n"
                + $"{Localization.T("Write: ")}{Rate(sample.WriteBytesPerSecond)}";
            if (running) await game.RetryAsync();
        }
        catch (Exception ex) { if (!closed) disk.Text = "Metrics unavailable: " + ex.Message; }
        finally { sampling = false; }
    }

    static string Rate(double? bytes) => bytes.HasValue ? $"{bytes.Value / 1048576:N1} MiB/s" : "N/A";

}
