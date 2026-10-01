
sealed partial class MainWindow
{
    UIElement BuildAbout()
    {
        var page = new StackPanel { Padding = new Thickness(28), Spacing = 20 };
        page.Children.Add(BuildHeader());
        var info = VisualTheme.Section("", "About DiscForge CHD");
        info.Children.Add(VisualTheme.Text(Localization.T("Convert PS1 and PS2 disc images to verified CHDs.")));
        var technologies = Localization.T("Built: ") + BuildInfo.Date
            + "\nC# 14 • .NET 10 • Windows x64"
            + "\nWinUI 3 / Windows App SDK 2.5.1 • WebView2 1.0.4191.47"
            + "\nSharpCompress 1.0.0"
            + "\n\nCHDman: MAME 0.289, 14/09/2026"
            + "\nC++20 / GCC 16.2 / Zen 3 / LTO. "
            + (Localization.IsPortuguese ? "Binário original preservado." : "Original binary preserved.")
            + "\n" + (Localization.IsPortuguese ? "Capas" : "Covers")
            + ": xlenore/ps2-covers, xlenore/psx-covers. "
            + (Localization.IsPortuguese ? "Os componentes de terceiros pertencem aos seus autores."
                : "Third-party components remain with their authors.");
        info.Children.Add(VisualTheme.Text(technologies));
        page.Children.Add(VisualTheme.Card(info));
        var report = new Button { Content = Localization.T("Report issue"), MinHeight = 40,
            HorizontalAlignment = HorizontalAlignment.Left };
        report.Click += async (_, _) => await ReportIssueAsync();
        page.Children.Add(report);
        aboutChanges.HorizontalAlignment = HorizontalAlignment.Stretch;
        aboutChanges.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        aboutChanges.Content = VisualTheme.Text(Localization.IsPortuguese
            ? AppChangelog.PortugueseText : AppChangelog.Text);
        page.Children.Add(aboutChanges);
        return new ScrollViewer { Content = page, Background = VisualTheme.Canvas };
    }
}
