using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

sealed partial class MainWindow
{
    readonly TextBox input = new() { PlaceholderText = "Pasta com seus jogos" };
    readonly TextBox output = new() { PlaceholderText = "Destino dos CHDs" };
    readonly ComboBox platform = new() { ItemsSource = new[] { "PS2", "PS1" }, SelectedIndex = 0 };
    readonly NumberBox threads = new()
    {
        Minimum = 1, Maximum = MachineInfo.LogicalProcessors, Value = MachineInfo.LogicalProcessors,
        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
    };
    readonly NumberBox cdHunk = new() { Value = 2448, Minimum = 2448, Maximum = 1048576 };
    readonly NumberBox dvdHunk = new() { Value = 2048, Minimum = 2048, Maximum = 1048576 };
    readonly TextBox cdCodecs = new() { Text = "cdlz,cdzs,cdzl,cdfl" };
    readonly TextBox dvdCodecs = new() { Text = "lzma,zstd,zlib,flac" };
    readonly CheckBox online = new() { Content = "Reconhecer jogo pelo serial", IsChecked = true };
    readonly CheckBox delete = new() { Content = "Apagar compactado após sucesso", IsChecked = false };
    readonly Button start = new() { Content = "Iniciar conversão", MinHeight = 42 };
    readonly Button stop = new() { Content = "Parar após atual", IsEnabled = false, MinHeight = 42 };
    readonly ProgressBar stage = new() { Minimum = 0, Maximum = 100 };
    readonly ProgressBar batch = new() { Minimum = 0, Maximum = 100 };
    readonly TextBlock status = VisualTheme.Text("Pronto para começar");
    readonly TextBlock batchStatus = VisualTheme.Text("Lote: 0%");
    readonly TextBlock elapsed = VisualTheme.Text("00:00:00", 25, true);
    readonly TextBlock cpu = VisualTheme.Text("Aguardando…", 25, true);
    readonly TextBlock disk = VisualTheme.Text("Aguardando…", 12);
    readonly TextBox log = new() { IsReadOnly = true, AcceptsReturn = true, Height = 170 };
    readonly StackPanel settings = new() { Spacing = 16 };
    readonly ContentControl settingsHost = new()
    {
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
    };
    readonly GamePanel game = new();
    readonly TabView tabs = new() { IsAddTabButtonVisible = false };
    readonly Expander advanced = new() { Header = "Ajustes de compressão" };
    readonly Expander aboutChanges = new() { Header = "Changelog" };
    readonly List<Button> helpButtons = new();
}
