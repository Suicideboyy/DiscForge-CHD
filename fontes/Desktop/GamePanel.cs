using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

sealed class GamePanel : Grid, IDisposable
{
    readonly WebView2 browser = new();
    readonly TextBlock fallback = new() { Text = "Loading game details…", TextWrapping = TextWrapping.Wrap };
    GameInfo current = new() { Status = "Waiting for input" };
    byte[] cover;
    bool currentPs2 = true;
    bool ready;
    bool initializing;
    bool disposed;
    bool loading;
    int generation;
    DateTime attempted;
    string expectedDocument = "";
    public string BrowserStatus { get; private set; } = "Initializing";

    public GamePanel()
    {
        Children.Add(browser);
        Children.Add(fallback);
        browser.Loaded += async (_, _) => await InitializeAsync();
    }

    async Task InitializeAsync()
    {
        if (ready || disposed || initializing) return;
        initializing = true;
        try
        {
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null,
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CHD Optimizer", "WebView2"), null);
            await browser.EnsureCoreWebView2Async(environment);
            if (disposed) return;
            for (int attempt = 0; attempt < 20 && browser.CoreWebView2 == null; attempt++)
                await Task.Delay(100);
            if (browser.CoreWebView2 == null)
                throw new InvalidOperationException("WebView2 did not initialize.");
            browser.CoreWebView2.Settings.IsScriptEnabled = false;
            browser.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            browser.CoreWebView2.Settings.IsWebMessageEnabled = false;
            browser.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;
            browser.CoreWebView2.NavigationStarting += (_, args) =>
            {
                args.Cancel = args.Uri != "about:blank" && args.Uri != expectedDocument;
                BrowserStatus = args.Cancel ? "External navigation blocked" : "Loading content";
            };
            browser.CoreWebView2.NavigationCompleted += (_, args) =>
                BrowserStatus = args.IsSuccess ? "OK" : "Navigation failed: " + args.WebErrorStatus;
            browser.CoreWebView2.DownloadStarting += (_, args) => args.Cancel = true;
            ready = true;
            BrowserStatus = "OK";
            fallback.Visibility = Visibility.Collapsed;
            Render();
        }
        catch (Exception ex)
        {
            BrowserStatus = "Unavailable: " + ex.Message;
            browser.Visibility = Visibility.Collapsed;
            fallback.Text = "Install Microsoft Edge WebView2 Runtime to display covers.\n" + current.Title;
        }
        finally { initializing = false; }
    }

    public void Reset()
    {
        generation++;
        loading = false;
        cover = null;
        current = new GameInfo { Status = "Waiting for input" };
        Render();
    }

    public void RefreshLanguage() => Render();

    public async Task UpdateAsync(GameInfo info, bool ps2)
    {
        string serial = MediaFiles.NormalizeSerial(info.Serial);
        if (serial.Length == 0) serial = MediaFiles.NormalizeSerial(info.Image + " " + info.Source);
        if (current.Serial != serial || currentPs2 != ps2
            || serial.Length == 0 && current.Source != info.Source)
        { generation++; cover = null; loading = false; }
        if (cover != null)
        {
            info.CoverProvider = current.CoverProvider;
            info.CoverUrl = current.CoverUrl;
        }
        currentPs2 = ps2;
        info.Serial = serial;
        current = info;
        Render();
        if (!loading && cover == null && (serial.Length > 0 || info.Title.Length > 0))
            await LoadCoverAsync();
    }

    public Task RetryAsync() => cover == null && !loading
        && (current.Serial.Length > 0 || current.Title.Length > 0)
        && DateTime.UtcNow - attempted > TimeSpan.FromSeconds(30) ? LoadCoverAsync() : Task.CompletedTask;

    // Ignore late replies so a previous game cover is never shown.
    async Task LoadCoverAsync()
    {
        int request = generation;
        loading = true;
        attempted = DateTime.UtcNow;
        CoverImage result = await CoverService.LoadDetails(current.Serial, current.Title,
            currentPs2, current.Source);
        if (disposed || request != generation) return;
        loading = false;
        if (result != null)
        {
            cover = result.Bytes;
            current.CoverProvider = result.Provider;
            current.CoverUrl = result.Url;
        }
        Render();
    }

    void Render()
    {
        if (disposed) return;
        fallback.Text = GameHtml.PlainText(current)
            + (BrowserStatus.StartsWith("Unavailable") ? "\nWebView2 Runtime unavailable." : "");
        if (ready)
        {
            string html = GameHtml.Render(current, cover, currentPs2);
            expectedDocument = "data:text/html;charset=utf-8;base64,"
                + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
            browser.NavigateToString(html);
        }
    }

    public void Dispose() { disposed = true; generation++; browser.Close(); }

    public async Task SavePreviewAsync(string path)
    {
        if (!ready) return;
        using var file = System.IO.File.Create(path);
        using var stream = System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(file);
        await browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
    }
}
