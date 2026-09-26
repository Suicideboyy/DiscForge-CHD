using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

sealed partial class MainWindow
{
    UIElement BuildAbout()
    {
        var page = new StackPanel { Padding = new Thickness(28), Spacing = 20 };
        page.Children.Add(BuildHeader());
        var info = VisualTheme.Section("", "Sobre o DiscForge CHD");
        info.Children.Add(VisualTheme.Text("Organize imagens de PS1 e PS2 em CHD, com verificação de integridade."));
        info.Children.Add(VisualTheme.Text(
            "Compilação: " + BuildInfo.Date + "\nC# 14 • .NET 10 • Windows x64"
            + "\nWinUI 3 / Windows App SDK 2.5.1 • WebView2 1.0.4191.47"
            + "\nSharpCompress 1.0.0 • 7-Zip de reserva"
            + "\n\nCHDman: MAME 0.289 (unknown), 14/09/2026"
            + "\nC++20 / GCC 16.2 / Zen 3 / LTO. Binário preservado."
            + "\nCapas: xlenore/ps2-covers. Componentes dos respectivos autores."));
        page.Children.Add(VisualTheme.Card(info));
        aboutChanges.HorizontalAlignment = HorizontalAlignment.Stretch;
        aboutChanges.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        aboutChanges.Content = VisualTheme.Text(AppChangelog.Text);
        page.Children.Add(aboutChanges);
        return new ScrollViewer { Content = page, Background = VisualTheme.Canvas };
    }
}
