
sealed partial class MainWindow
{
    // One height for every editor, so a settings row never shows mixed metrics.
    readonly TextBox input = new() { PlaceholderText = "Folder containing your games", MinHeight = VisualTheme.ControlSmall };
    readonly TextBox output = new() { PlaceholderText = "CHD destination", MinHeight = VisualTheme.ControlSmall };
    readonly ComboBox platform = new()
    {
        ItemsSource = new[] { "PS2", "PS1" }, SelectedIndex = 0, MinHeight = VisualTheme.ControlSmall
    };
    readonly NumberBox threads = new()
    {
        Minimum = 1, Maximum = MachineInfo.LogicalProcessors, Value = MachineInfo.LogicalProcessors,
        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
        MinHeight = VisualTheme.ControlSmall
    };
    readonly NumberBox cdHunk = new()
    {
        Value = 2448, Minimum = 2448, Maximum = 1048576, MinHeight = VisualTheme.ControlSmall
    };
    readonly NumberBox dvdHunk = new()
    {
        Value = 2048, Minimum = 2048, Maximum = 1048576, MinHeight = VisualTheme.ControlSmall
    };
    readonly StackPanel cdCodecs = new() { Spacing = 4 };
    readonly StackPanel dvdCodecs = new() { Spacing = 4 };
    readonly List<CheckBox> cdCodecChoices = new();
    readonly List<CheckBox> dvdCodecChoices = new();
    readonly CheckBox online = new() { Content = "Identify games by serial", IsChecked = true };
    readonly CheckBox delete = new() { Content = "Remove archive after success", IsChecked = false };
    readonly CheckBox autoDetect = new() { Content = "Auto-detect system", IsChecked = false };
    readonly CheckBox legacyCompatibility = new() { IsChecked = false };
    readonly ComboBox language = new()
    {
        ItemsSource = new[] { "English", "Português (Brasil)" }, SelectedIndex = 0,
        MinHeight = VisualTheme.ControlSmall
    };
    // MinHeight is owned by the VisualTheme button roles applied in BuildProgress.
    readonly Button start = new() { Content = "Start conversion" };
    readonly Button stop = new() { Content = "Stop after current", IsEnabled = false };
    readonly Button stopNow = new() { Content = "Stop now", IsEnabled = false };
    readonly TextBox chdmanPath = new()
    {
        PlaceholderText = "Automatic (bundled)", MinHeight = VisualTheme.ControlSmall
    };
    readonly ProgressBar stage = new() { Minimum = 0, Maximum = 100 };
    readonly ProgressBar batch = new() { Minimum = 0, Maximum = 100 };
    readonly TextBlock status = VisualTheme.Text("Ready to start");
    readonly TextBlock batchStatus = VisualTheme.Text("Batch: 0%");
    readonly TextBlock elapsed = VisualTheme.Text("00:00:00", 25, true);
    readonly TextBlock cpu = VisualTheme.Text("Waiting…", 25, true);
    readonly TextBlock disk = VisualTheme.Text("Waiting…", 13);
    // Command row. Kept as fields so the page reflow can restack the buttons when
    // the column cannot hold them, and the smoke run can assert both.
    readonly StackPanel actions = new() { Spacing = VisualTheme.GapS };
    readonly Button openOutput = new();
    readonly TextBox log = new() { IsReadOnly = true, AcceptsReturn = true, Height = 170 };
    readonly StackPanel settings = new() { Spacing = 16 };
    readonly ContentControl settingsHost = new()
    {
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
    };
    readonly GamePanel game = new();
    readonly TelemetryPanel telemetry = new();
    // Shell chrome. NavigationView keeps an open pane open at any width, so the
    // pane state is driven from navigation.ActualWidth against PaneExpandWidth.
    readonly NavigationView navigation = new()
    {
        IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
        IsSettingsVisible = false,
        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
        IsPaneToggleButtonVisible = true
    };
    bool lastExpandedPane = true;
    Grid titleBar;
    // Kept so the smoke run can assert the Bento reflow instead of eyeballing pixels.
    Border gameCard;
    // The settings cards and the row that holds them, for the same reason.
    Grid settingsPanels;
    FrameworkElement[] settingsCards = Array.Empty<FrameworkElement>();
    // Reflow latches: each keeps the last applied state so a size change that only
    // toggles the scrollbar cannot start a reflow loop.
    bool? lastStacked, lastStackedTiles, lastStackedCaptions, lastStackedCards;
    // Kept so the smoke run can verify the new legend strings in both languages.
    TextBlock stageLegend;
    // Equal-width rows, each of which restacks into one column when its own
    // measured width can no longer hold their content side by side.
    Grid metricRow;
    Grid legend;
    readonly FrameworkElement[] metricTiles = new FrameworkElement[3];
    readonly FrameworkElement[] legendItems = new FrameworkElement[3];
    UIElement[] pages = Array.Empty<UIElement>();
    bool micaActive;
    readonly Expander advanced = new() { Header = "Encoding options" };
    readonly Expander aboutChanges = new() { Header = "Changelog" };
    readonly List<Button> helpButtons = new();
    readonly Button resetDefaults = new() { Content = "Reset defaults" };
}
