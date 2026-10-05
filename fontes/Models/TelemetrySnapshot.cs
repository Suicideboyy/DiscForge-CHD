
// Plotly telemetry messages: C# snapshots flow one way to telemetry.html via
// CoreWebView2.PostWebMessageAsJson. The JavaScript side owns history (90 points).
// Field names stay PascalCase so telemetry.js reads them without translation.

sealed class TelemetrySnapshot
{
    public string Type => "sample";
    public DateTime Timestamp { get; set; }
    public double? CpuPercent { get; set; }
    public double? ReadMiBps { get; set; }
    public double? WriteMiBps { get; set; }
    public double StagePercent { get; set; }
    public double BatchPercent { get; set; }

    public static TelemetrySnapshot From(PerformanceSample sample, double stage, double batch)
    {
        const double Mebibyte = 1048576.0;
        return new TelemetrySnapshot
        {
            Timestamp = DateTime.UtcNow,
            CpuPercent = sample.CpuPercent,
            ReadMiBps = sample.ReadBytesPerSecond / Mebibyte,
            WriteMiBps = sample.WriteBytesPerSecond / Mebibyte,
            StagePercent = stage,
            BatchPercent = batch
        };
    }
}

// Theme tokens sent once the telemetry document is ready (and on HighContrast change).
// Keeps Plotly colors aligned with VisualTheme without hardcoding palettes in JS.
sealed class TelemetryTheme
{
    public string Type => "theme";
    public string Accent { get; set; } = "#A78BFA";
    public string Amber { get; set; } = "#FBBF24";
    public string Teal { get; set; } = "#5EEAD4";
    public string Ink { get; set; } = "#E2E8F0";
    public string Muted { get; set; } = "#94A3B8";
    public string Surface { get; set; } = "#1E1C35";
    public bool HighContrast { get; set; }

    static string Hex(Microsoft.UI.Xaml.Media.SolidColorBrush brush)
    {
        var color = brush.Color;
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static TelemetryTheme Current()
    {
        return new TelemetryTheme
        {
            Accent = Hex(VisualTheme.Accent),
            Amber = "#FBBF24",
            Teal = Hex(VisualTheme.Teal),
            Ink = Hex(VisualTheme.Ink),
            Muted = Hex(VisualTheme.Muted),
            Surface = Hex(VisualTheme.Surface),
            HighContrast = VisualTheme.HighContrast
        };
    }
}
