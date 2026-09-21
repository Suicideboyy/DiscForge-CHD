using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

partial class MainForm
{
    readonly Stopwatch taskClock = new Stopwatch();
    readonly SystemPerformance performance = new SystemPerformance();
    readonly Timer telemetryTimer = new Timer();
    readonly Label elapsedValue = new Label();
    readonly Label cpuValue = new Label();
    readonly Label diskValue = new Label();
    readonly Label diskDetail = new Label();
    bool sampling;

    Control BuildTelemetry()
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        row.Controls.Add(MetricCard("TEMPO DA ENTRADA ATUAL", elapsedValue, null, Theme.Purple));
        row.Controls.Add(MetricCard("CPU • SISTEMA", cpuValue, null, Theme.Teal));
        row.Controls.Add(MetricCard("DISCOS • SISTEMA", diskValue, diskDetail, Theme.Navy));
        elapsedValue.Text = "00:00:00";
        cpuValue.Text = "Aguardando…";
        diskValue.Text = "Leitura —   Escrita —";
        diskValue.Font = new Font("Segoe UI Semibold", 12);
        diskDetail.Text = "Coletando atividade dos discos";
        telemetryTimer.Interval = 1000;
        telemetryTimer.Tick += async delegate { await RefreshTelemetry(); };
        Shown += delegate { telemetryTimer.Start(); };
        return row;
    }

    static Control MetricCard(string title, Label value, Label detail, Color accent)
    {
        var card = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 9, 16, 6) };
        card.BackColor = Color.FromArgb((accent.R + 2295) / 10,
            (accent.G + 2295) / 10, (accent.B + 2295) / 10);
        var heading = new Label
        {
            Text = title, Dock = DockStyle.Top, Height = 20,
            ForeColor = Theme.Muted, Font = new Font("Segoe UI Semibold", 8)
        };
        value.Dock = DockStyle.Top;
        value.Height = 29;
        value.ForeColor = accent;
        value.Font = new Font("Segoe UI Semibold", 19);
        if (detail != null)
        {
            detail.Dock = DockStyle.Bottom;
            detail.Height = 17;
            detail.Font = new Font("Segoe UI", 8);
            detail.ForeColor = Theme.Muted;
            card.Controls.Add(detail);
        }
        card.Controls.Add(value);
        card.Controls.Add(heading);
        return card;
    }

    async Task RefreshTelemetry()
    {
        UpdateElapsed();
        if (sampling || IsDisposed)
        {
            return;
        }
        sampling = true;
        try
        {
            PerformanceSample sample = await Task.Run(() => performance.Read());
            if (IsDisposed)
            {
                return;
            }
            cpuValue.Text = sample.CpuPercent.HasValue ? sample.CpuPercent.Value.ToString("N1") + "%" : "N/D";
            diskValue.Text = "L " + Rate(sample.ReadBytesPerSecond) + "   E " + Rate(sample.WriteBytesPerSecond);
            diskDetail.Text = sample.DiskBusyPercent.HasValue
                ? "Atividade média: " + sample.DiskBusyPercent.Value.ToString("N1") + "% • todos os discos"
                : "Contadores de disco indisponíveis";
        }
        catch
        {
            if (!IsDisposed)
            {
                cpuValue.Text = "N/D";
                diskValue.Text = "Métricas indisponíveis";
            }
        }
        finally
        {
            sampling = false;
        }
        if (!IsDisposed && running && cover.Image == null && !coverLoading
            && DateTime.UtcNow - lastCoverAttempt > TimeSpan.FromSeconds(30))
        {
            await RefreshCover(coverSerial);
        }
    }

    static string Rate(double? bytes)
    {
        return bytes.HasValue ? (bytes.Value / 1048576).ToString("N1") + " MiB/s" : "N/D";
    }

    void UpdateElapsed()
    {
        TimeSpan elapsed = taskClock.Elapsed;
        elapsedValue.Text = String.Format("{0:00}:{1:00}:{2:00}",
            (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            telemetryTimer.Stop();
            telemetryTimer.Dispose();
            performance.Dispose();
            if (cover.Image != null)
            {
                cover.Image.Dispose();
                cover.Image = null;
            }
        }
        base.Dispose(disposing);
    }
}
