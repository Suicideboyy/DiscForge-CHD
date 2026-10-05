using Microsoft.Web.WebView2.Core;

// Single shared WebView2 environment (same user-data folder as GamePanel).
// A second environment on the same folder races at startup and doubles the
// browser process cost; settings stay per-control, so the game panel keeps
// scripts disabled while telemetry enables them.
static class WebView2Boot
{
    static readonly object Gate = new();
    static Task<CoreWebView2Environment> cached;

    public static Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        lock (Gate)
        {
            // Never cache a faulted environment: a transient runtime failure
            // must not condemn both panels until the app restarts.
            if (cached is { IsCompleted: true } && (cached.IsFaulted || cached.IsCanceled))
                cached = null;
            return cached ??= CreateAsync();
        }

        static async Task<CoreWebView2Environment> CreateAsync()
        {
            return await CoreWebView2Environment.CreateWithOptionsAsync(null,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CHD Optimizer", "WebView2"), null);
        }
    }
}
