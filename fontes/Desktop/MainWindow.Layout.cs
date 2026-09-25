using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;

sealed partial class MainWindow : Window
{
    readonly TextBox input = new() { Header = "Pasta de entrada", PlaceholderText = "Selecione os jogos" };
    readonly TextBox output = new() { Header = "Pasta de saída", PlaceholderText = "Selecione o destino dos CHDs" };
    readonly ComboBox platform = new() { Header = "Plataforma", ItemsSource = new[] { "PS2", "PS1" }, SelectedIndex = 0 };
    readonly NumberBox threads = new() { Header = "Threads (automático)", Minimum = 1,
        Maximum = MachineInfo.LogicalProcessors, Value = MachineInfo.LogicalProcessors };
    readonly NumberBox cdHunk = new() { Header = "Hunk CD", Value = 2448, Minimum = 2448, Maximum = 1048576 };
    readonly NumberBox dvdHunk = new() { Header = "Hunk DVD", Value = 2048, Minimum = 2048, Maximum = 1048576 };
    readonly TextBox cdCodecs = new() { Header = "Codecs CD", Text = "cdlz,cdzs,cdzl,cdfl" };
    readonly TextBox dvdCodecs = new() { Header = "Codecs DVD", Text = "lzma,zstd,zlib,flac" };
    readonly CheckBox online = new() { Content = "Consultar serial e nome", IsChecked = true };
    readonly CheckBox delete = new() { Content = "Apagar compactado após verificar o CHD", IsChecked = false };
    readonly Button start = new() { Content = "Iniciar conversão" };
    readonly Button stop = new() { Content = "Parar após a entrada atual", IsEnabled = false };
    readonly ProgressBar stage = new() { Minimum = 0, Maximum = 100 };
    readonly ProgressBar batch = new() { Minimum = 0, Maximum = 100 };
    readonly TextBlock status = new() { Text = "Pronto para começar", TextWrapping = TextWrapping.Wrap };
    readonly TextBlock batchStatus = new() { Text = "Lote: 0%", TextWrapping = TextWrapping.Wrap };
    readonly TextBlock elapsed = new() { Text = "00:00:00", FontSize = 23 };
    readonly TextBlock cpu = new() { Text = "Aguardando…", FontSize = 23 };
    readonly TextBlock disk = new() { Text = "Aguardando…", TextWrapping = TextWrapping.Wrap };
    readonly TextBox log = new() { IsReadOnly = true, AcceptsReturn = true, Height = 190,
        TextWrapping = TextWrapping.Wrap };
    readonly StackPanel settings = new() { Spacing = 12 };
    readonly ContentControl settingsHost = new();
    readonly GamePanel game = new();

    public MainWindow(string[] args)
    {
        Title = AppInfo.DisplayName;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1320, 1000));
        var tabs = new TabView { IsAddTabButtonVisible = false,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 247, 247, 252)) };
        tabs.TabItems.Add(new TabViewItem { Header = "Conversão", IsClosable = false, Content = BuildConversion() });
        tabs.TabItems.Add(new TabViewItem { Header = "Sobre", IsClosable = false, Content = BuildAbout() });
        Content = tabs;
        start.Click += async (_, _) => await StartAsync();
        stop.Click += (_, _) => RequestStop();
        platform.SelectionChanged += (_, _) => { UpdatePlatform(); game.Reset(); };
        AppWindow.Closing += (_, e) =>
        {
            if (running) { e.Cancel = true; RequestStop(); status.Text = "Aguarde a entrada terminar antes de fechar."; }
        };
        Closed += (_, _) => { closed = true; timer.Stop(); performance.Dispose(); game.Dispose(); };
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += async (_, _) => await RefreshTelemetry();
        timer.Start();
        UpdatePlatform();
        if (args.Length == 2 && args[0] == "--ui-smoke") RunSmoke(args[1]);
    }

    UIElement BuildConversion()
    {
        var columns = new Grid { ColumnSpacing = 20, Padding = new Thickness(24),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 247, 247, 252)) };
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = new StackPanel { Spacing = 14 };
        left.Children.Add(new TextBlock { Text = "Sua coleção, organizada em CHD", FontSize = 28, FontWeight =
            Microsoft.UI.Text.FontWeights.SemiBold });
        var metrics = new Grid { ColumnSpacing = 8 };
        foreach (var item in new[] { Metric("TEMPO DA ENTRADA", elapsed), Metric("CPU DO SISTEMA", cpu),
            Metric("DISCOS DO SISTEMA", disk) })
        {
            int index = metrics.Children.Count;
            metrics.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(item, index);
            metrics.Children.Add(item);
        }
        left.Children.Add(metrics);
        settings.Children.Add(PathRow(input));
        settings.Children.Add(PathRow(output));
        settings.Children.Add(Pair(platform, threads));
        settings.Children.Add(Pair(cdHunk, dvdHunk));
        settings.Children.Add(Pair(cdCodecs, dvdCodecs));
        settings.Children.Add(online);
        settings.Children.Add(delete);
        settingsHost.Content = settings;
        left.Children.Add(settingsHost);
        left.Children.Add(batchStatus);
        left.Children.Add(batch);
        left.Children.Add(status);
        left.Children.Add(stage);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        start.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        start.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 80, 200));
        actions.Children.Add(start);
        actions.Children.Add(stop);
        var open = new Button { Content = "Abrir saída" };
        open.Click += (_, _) =>
        {
            if (System.IO.Directory.Exists(output.Text))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(output.Text)
                    { UseShellExecute = true });
        };
        actions.Children.Add(open);
        left.Children.Add(actions);
        left.Children.Add(log);
        columns.Children.Add(new ScrollViewer { Content = left, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Grid.SetColumn(game, 1);
        columns.Children.Add(game);
        return columns;
    }

    static Border Metric(string heading, UIElement value)
    {
        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(new TextBlock { Text = heading, FontSize = 11 });
        body.Children.Add(value);
        return new Border { CornerRadius = new CornerRadius(16), Padding = new Thickness(16),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 230, 252)),
            Child = body, MinHeight = 106, RequestedTheme = ElementTheme.Light };
    }

    static Grid Pair(FrameworkElement a, FrameworkElement b)
    {
        var row = new Grid { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition());
        a.HorizontalAlignment = b.HorizontalAlignment = HorizontalAlignment.Stretch;
        row.Children.Add(a);
        Grid.SetColumn(b, 1);
        row.Children.Add(b);
        return row;
    }

    Grid PathRow(TextBox box)
    {
        var button = new Button { Content = "Escolher…", VerticalAlignment = VerticalAlignment.Bottom };
        button.Click += async (_, _) =>
        {
            try
            {
                var picker = new FolderPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
                picker.FileTypeFilter.Add("*");
                var folder = await picker.PickSingleFolderAsync();
                if (folder != null) box.Text = folder.Path;
            }
            catch (Exception ex) { Append("Não foi possível abrir a seleção: " + ex.Message); }
        };
        var row = Pair(box, button);
        row.ColumnDefinitions[1].Width = GridLength.Auto;
        return row;
    }

    void UpdatePlatform()
    {
        bool ps2 = (string)platform.SelectedItem == "PS2";
        dvdHunk.IsEnabled = dvdCodecs.IsEnabled = online.IsEnabled = delete.IsEnabled = ps2;
    }

    void SetSettingsEnabled(bool enabled)
    {
        start.IsEnabled = enabled;
        settingsHost.IsEnabled = enabled;
        if (enabled) UpdatePlatform();
    }

    static UIElement BuildAbout()
    {
        var changes = new Expander { Header = "Changelog", HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = new TextBlock { Text = AppChangelog.Text, TextWrapping = TextWrapping.Wrap } };
        var body = new StackPanel { Padding = new Thickness(32), Spacing = 20 };
        body.Children.Add(new TextBlock { Text = AppInfo.DisplayName, FontSize = 30 });
        body.Children.Add(new TextBlock { Text = "Compilação: " + BuildInfo.Date + "\nC# 14 • .NET 10 • Windows x64"
            + "\nWinUI 3 / Windows App SDK 2.5.1 • WebView2 1.0.4191.47"
            + "\nSharpCompress 1.0.0 • 7-Zip de reserva"
            + "\n\nCHDman: MAME 0.289 (unknown), compilado em 14/09/2026"
            + "\nC++20 / GCC 16.2 / Zen 3 / LTO. Binário preservado."
            + "\nCapas: xlenore/ps2-covers. Componentes pertencem aos respectivos autores.",
            TextWrapping = TextWrapping.Wrap });
        body.Children.Add(changes);
        return new ScrollViewer { Content = body,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 247, 247, 252)) };
    }
}
