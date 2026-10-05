using Microsoft.Web.WebView2.Core;

// Single WebView2 hosting the Plotly telemetry strip (CPU / disk / stage sparklines).
// Values stay in native WinUI TextBlocks; this panel is a decorative, aria-hidden
// chart layer fed one way: C# -> PostWebMessageAsJson -> telemetry.js.
// On any WebView2 failure the panel collapses and textual metrics keep working.
sealed class TelemetryPanel : Grid, IDisposable
{
    const string DocumentUrl = "https://appassets/telemetry.html";

    readonly WebView2 browser = new() { DefaultBackgroundColor = VisualTheme.Surface.Color };
    bool ready;
    bool initializing;
    bool disposed;
    bool themeSent;
    bool lastHighContrast;
    string lastError = "";

    public string Status { get; private set; } = "Initializing";

    public TelemetryPanel()
    {
        browser.IsTabStop = false;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(browser,
            Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        Children.Add(browser);
        browser.Loaded += async (_, _) => await InitializeAsync();
    }

    async Task InitializeAsync()
    {
        if (ready || disposed || initializing) return;
        initializing = true;
        try
        {
            var environment = await WebView2Boot.GetEnvironmentAsync();
            await browser.EnsureCoreWebView2Async(environment);
            if (disposed) return;
            for (int attempt = 0; attempt < 20 && browser.CoreWebView2 == null; attempt++)
                await Task.Delay(100);
            if (browser.CoreWebView2 == null)
                throw new InvalidOperationException("WebView2 did not initialize.");
            var settings = browser.CoreWebView2.Settings;
            settings.IsScriptEnabled = true;
            settings.AreHostObjectsAllowed = false;
            settings.IsWebMessageEnabled = true;
            settings.AreDefaultContextMenusEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.AreDefaultScriptDialogsEnabled = false;
            settings.IsZoomControlEnabled = false;
            settings.IsGeneralAutofillEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
#if !DEBUG
            settings.AreDevToolsEnabled = false;
#endif
            browser.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;
            browser.CoreWebView2.NavigationStarting += (_, args) =>
            {
                // Local-only: the mapped folder holds our own files; anything
                // else (including normalization variants) never navigates.
                bool local = args.Uri == "about:blank"
                    || args.Uri.StartsWith("https://appassets/", StringComparison.OrdinalIgnoreCase);
                args.Cancel = !local;
                if (args.Cancel) Status = "External navigation blocked";
            };
            browser.CoreWebView2.NavigationCompleted += (sender, e) =>
            {
                if (disposed) return;
                if (e.IsSuccess)
                {
                    ready = true;
                    Status = "OK";
                    _ = SendThemeAsync();
                }
                else Status = "Navigation failed: " + e.WebErrorStatus;
            };
            browser.CoreWebView2.DownloadStarting += (_, args) => args.Cancel = true;
            string plotlyDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Plotly");
            if (!Directory.Exists(plotlyDir))
                throw new DirectoryNotFoundException("Telemetry assets are missing.");
            browser.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", plotlyDir,
                CoreWebView2HostResourceAccessKind.Allow);
            browser.CoreWebView2.Navigate(DocumentUrl);
        }
        catch (Exception ex)
        {
            Status = "Unavailable: " + ex.Message;
            lastError = ex.ToString();
            CrashReporter.Record(ex, "TelemetryPanel.Initialize");
            Visibility = Visibility.Collapsed;
        }
        finally { initializing = false; }
    }

    Task SendThemeAsync()
    {
        themeSent = true;
        lastHighContrast = VisualTheme.HighContrast;
        return PostAsync(JsonSerializer.Serialize(TelemetryTheme.Current()));
    }

    public Task PostSnapshotAsync(TelemetrySnapshot snapshot)
    {
        // HighContrast has no reliable change event here; re-send theme tokens
        // when the system state flips. Snapshots never wait for the theme.
        if (ready && (!themeSent || VisualTheme.HighContrast != lastHighContrast))
            _ = SendThemeAsync();
        return PostAsync(JsonSerializer.Serialize(snapshot));
    }

    // Never throws: telemetry must not break the 1s UI timer or conversion.
    async Task PostAsync(string json)
    {
        if (!ready || disposed || browser.CoreWebView2 == null) return;
        try
        {
            if (DispatcherQueue.HasThreadAccess)
                browser.CoreWebView2.PostWebMessageAsJson(json);
            else
            {
                var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        if (ready && !disposed && browser.CoreWebView2 != null)
                            browser.CoreWebView2.PostWebMessageAsJson(json);
                        sent.SetResult();
                    }
                    catch (Exception ex) { lastError = ex.Message; sent.SetResult(); }
                });
                await sent.Task;
            }
        }
        catch (Exception ex) { lastError = ex.Message; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ready = false;
        try { browser.Close(); } catch { }
    }

    public async Task SavePreviewAsync(string path)
    {
        if (!ready || disposed || browser.CoreWebView2 == null) return;
        try
        {
            using var file = File.Create(path);
            using var stream = System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(file);
            await browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
        }
        catch { /* smoke-only capture must never fail the diagnostics run */ }
    }
}
