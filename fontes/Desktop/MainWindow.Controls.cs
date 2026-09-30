using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

sealed partial class MainWindow
{
    readonly TextBox input = new() { PlaceholderText = "Folder containing your games" };
    readonly TextBox output = new() { PlaceholderText = "CHD destination" };
    readonly ComboBox platform = new() { ItemsSource = new[] { "PS2", "PS1" }, SelectedIndex = 0 };
    readonly NumberBox threads = new()
    {
        Minimum = 1, Maximum = MachineInfo.LogicalProcessors, Value = MachineInfo.LogicalProcessors,
        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
    };
    readonly NumberBox cdHunk = new() { Value = 2448, Minimum = 2448, Maximum = 1048576 };
    readonly NumberBox dvdHunk = new() { Value = 2048, Minimum = 2048, Maximum = 1048576 };
    readonly StackPanel cdCodecs = new() { Spacing = 4 };
    readonly StackPanel dvdCodecs = new() { Spacing = 4 };
    readonly List<CheckBox> cdCodecChoices = new();
    readonly List<CheckBox> dvdCodecChoices = new();
    readonly CheckBox online = new() { Content = "Identify games by serial", IsChecked = true };
    readonly CheckBox delete = new() { Content = "Remove archive after success", IsChecked = false };
    readonly CheckBox autoDetect = new() { Content = "Auto-detect system", IsChecked = false };
    readonly CheckBox legacyCompatibility = new() { IsChecked = false };
    readonly ComboBox language = new() { ItemsSource = new[] { "English", "Português (Brasil)" }, SelectedIndex = 0 };
    readonly Button start = new() { Content = "Start conversion", MinHeight = 42 };
    readonly Button stop = new() { Content = "Stop after current", IsEnabled = false, MinHeight = 42 };
    readonly Button stopNow = new() { Content = "Stop now", IsEnabled = false, MinHeight = 42 };
    readonly TextBox chdmanPath = new() { PlaceholderText = "Automatic (bundled)" };
    readonly CheckBox ramExtraction = new() { IsChecked = false };
    readonly TextBox ramDiskPath = new();
    readonly TextBlock ramStatus = VisualTheme.Text("", 12);
    readonly ProgressBar stage = new() { Minimum = 0, Maximum = 100 };
    readonly ProgressBar batch = new() { Minimum = 0, Maximum = 100 };
    readonly TextBlock status = VisualTheme.Text("Ready to start");
    readonly TextBlock batchStatus = VisualTheme.Text("Batch: 0%");
    readonly TextBlock elapsed = VisualTheme.Text("00:00:00", 25, true);
    readonly TextBlock cpu = VisualTheme.Text("Waiting…", 25, true);
    readonly TextBlock disk = VisualTheme.Text("Waiting…", 12);
    readonly TextBox log = new() { IsReadOnly = true, AcceptsReturn = true, Height = 170 };
    readonly StackPanel settings = new() { Spacing = 16 };
    readonly ContentControl settingsHost = new()
    {
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
    };
    readonly GamePanel game = new();
    readonly TabView tabs = new() { IsAddTabButtonVisible = false };
    readonly Expander advanced = new() { Header = "Encoding options" };
    readonly Expander aboutChanges = new() { Header = "Changelog" };
    readonly List<Button> helpButtons = new();
    readonly Button resetDefaults = new() { Content = "Reset defaults" };
}
