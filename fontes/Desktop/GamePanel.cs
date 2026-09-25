using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

sealed class GamePanel : Grid, IDisposable
{
    readonly WebView2 browser = new();
    readonly TextBlock fallback = new() { Text = "Carregando painel do jogo…", TextWrapping = TextWrapping.Wrap };
    GameInfo current = new() { Status = "Aguardando entrada" };
    byte[] cover;
    bool ready;
    bool disposed;
    bool loading;
    int generation;
    DateTime attempted;
    string expectedDocument = "";
    public string BrowserStatus { get; private set; } = "Inicializando";

    public GamePanel()
    {
        Children.Add(browser);
        Children.Add(fallback);
        Loaded += async (_, _) => await InitializeAsync();
    }

    async Task InitializeAsync()
    {
        if (ready || disposed) return;
        try
        {
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null,
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CHD Optimizer", "WebView2"), null);
            await browser.EnsureCoreWebView2Async(environment);
            if (disposed) return;
            browser.CoreWebView2.Settings.IsScriptEnabled = false;
            browser.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            browser.CoreWebView2.Settings.IsWebMessageEnabled = false;
            browser.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;
            browser.CoreWebView2.NavigationStarting += (_, args) =>
            {
                args.Cancel = args.Uri != "about:blank" && args.Uri != expectedDocument;
                BrowserStatus = args.Cancel ? "Navegação externa bloqueada" : "Carregando conteúdo";
            };
            browser.CoreWebView2.NavigationCompleted += (_, args) =>
                BrowserStatus = args.IsSuccess ? "OK" : "Falha de navegação: " + args.WebErrorStatus;
            browser.CoreWebView2.DownloadStarting += (_, args) => args.Cancel = true;
            ready = true;
            BrowserStatus = "OK";
            fallback.Visibility = Visibility.Collapsed;
            Render();
        }
        catch (Exception ex)
        {
            BrowserStatus = "Indisponível: " + ex.Message;
            browser.Visibility = Visibility.Collapsed;
            fallback.Text = "Instale o Microsoft Edge WebView2 Runtime para exibir capas.\n" + current.Title;
        }
    }

    public void Reset()
    {
        generation++;
        loading = false;
        cover = null;
        current = new GameInfo { Status = "Aguardando entrada" };
        Render();
    }

    public async Task UpdateAsync(GameInfo info, bool ps2)
    {
        string serial = ps2 ? MediaFiles.NormalizeSerial(info.Serial) : "";
        if (ps2 && serial.Length == 0) serial = MediaFiles.NormalizeSerial(info.Image + " " + info.Source);
        if (current.Serial != serial) { generation++; cover = null; loading = false; }
        info.Serial = serial;
        current = info;
        Render();
        if (!loading && serial.Length > 0) await LoadCoverAsync();
    }

    public Task RetryAsync() => cover == null && !loading && current.Serial.Length > 0
        && DateTime.UtcNow - attempted > TimeSpan.FromSeconds(30) ? LoadCoverAsync() : Task.CompletedTask;

    async Task LoadCoverAsync()
    {
        int request = generation;
        loading = true;
        attempted = DateTime.UtcNow;
        byte[] result = await CoverService.Load(current.Serial);
        if (disposed || request != generation) return;
        loading = false;
        if (result != null) cover = result;
        Render();
    }

    void Render()
    {
        if (disposed) return;
        fallback.Text = current.Title + "\n" + current.Serial + " • " + current.Type + "\n" + current.Status
            + "\n" + current.Detail + (BrowserStatus.StartsWith("Indisponível") ? "\nWebView2 Runtime indisponível." : "");
        if (ready)
        {
            string html = GameHtml.Render(current, cover);
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
