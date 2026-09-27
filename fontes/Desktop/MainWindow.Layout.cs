using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

sealed partial class MainWindow : Window
{
    public MainWindow(string[] args)
    {
        Title = AppInfo.DisplayName;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1380, 1000));
        string icon = Path.Combine(AppContext.BaseDirectory, "Assets", "discforge.ico");
        if (File.Exists(icon)) AppWindow.SetIcon(icon);
        tabs.Background = VisualTheme.Canvas;
        tabs.TabItems.Add(new TabViewItem { Header = "Conversion", IsClosable = false,
            IconSource = new SymbolIconSource { Symbol = Symbol.Play }, Content = BuildConversion() });
        tabs.TabItems.Add(new TabViewItem { Header = "Settings", IsClosable = false,
            IconSource = new SymbolIconSource { Symbol = Symbol.Setting }, Content = BuildToolSettings() });
        tabs.TabItems.Add(new TabViewItem { Header = "About", IsClosable = false,
            IconSource = new SymbolIconSource { Symbol = Symbol.Help }, Content = BuildAbout() });
        Content = tabs;
        LoadPreferences();
        ConnectEvents();
        if (args.Length == 2 && args[0] == "--ui-smoke") RunSmoke(args[1]);
    }

    /// <summary>Builds the workspace and stacks the game panel in narrow windows.</summary>
    UIElement BuildConversion()
    {
        var page = new StackPanel { Spacing = 20, Padding = new Thickness(24) };
        page.ChildrenTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection
        {
            new Microsoft.UI.Xaml.Media.Animation.EntranceThemeTransition()
        };
        page.Children.Add(BuildHeader());
        var columns = new Grid { ColumnSpacing = 20, RowSpacing = 20 };
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });
        columns.ColumnDefinitions.Add(new ColumnDefinition());
        columns.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        columns.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var work = new StackPanel { Spacing = 16 };
        work.Children.Add(BuildMetrics());
        BuildSettings();
        settingsHost.Content = settings;
        work.Children.Add(settingsHost);
        work.Children.Add(BuildProgress());
        columns.Children.Add(work);
        var details = VisualTheme.Card(game, 0);
        details.Height = 810;
        details.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(details, 1);
        columns.Children.Add(details);
        page.Children.Add(columns);
        page.SizeChanged += (_, _) =>
        {
            bool narrow = page.ActualWidth < 1040;
            Grid.SetColumnSpan(work, narrow ? 2 : 1);
            Grid.SetRow(details, narrow ? 1 : 0);
            Grid.SetColumn(details, narrow ? 0 : 1);
            Grid.SetColumnSpan(details, narrow ? 2 : 1);
            details.Height = narrow ? 640 : 810;
        };
        return new ScrollViewer
        {
            Content = page,
            Background = VisualTheme.Canvas,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    UIElement BuildHeader()
    {
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        brand.Children.Add(new Image
        {
            Source = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "discforge.png"))),
            Width = 62, Height = 62
        });
        var name = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
        var title = VisualTheme.Text(AppInfo.Name, 29, true);
        title.Foreground = VisualTheme.White;
        name.Children.Add(title);
        var subtitle = VisualTheme.Text("Your collection. Less space. Every experience.", 13);
        subtitle.Foreground = VisualTheme.Brush(183, 199, 222);
        name.Children.Add(subtitle);
        brand.Children.Add(name);
        var version = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 6 };
        var release = VisualTheme.Text("VERSION " + AppInfo.Version, 12, true);
        release.Foreground = VisualTheme.Brush(153, 234, 217);
        var date = VisualTheme.Text("Built on " + BuildInfo.Date, 12);
        date.Foreground = VisualTheme.Brush(206, 217, 236);
        version.Children.Add(release);
        version.Children.Add(date);
        var row = VisualTheme.Pair(brand, version);
        row.ColumnDefinitions[1].Width = GridLength.Auto;
        return new Border
        {
            Background = VisualTheme.Navy, CornerRadius = new CornerRadius(20),
            Padding = new Thickness(24, 18, 24, 18), Child = row
        };
    }

    UIElement BuildMetrics()
    {
        var row = new Grid { ColumnSpacing = 10 };
        var cards = new[]
        {
            Metric("CURRENT INPUT", elapsed, OptionHelp.Time, VisualTheme.Accent),
            Metric("CPU · APP", cpu, OptionHelp.Cpu, VisualTheme.Teal),
            Metric("DISK · APP", disk, OptionHelp.Disk, VisualTheme.Brush(222, 133, 42))
        };
        foreach (var card in cards)
        {
            row.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(card, row.Children.Count);
            row.Children.Add(card);
        }
        return row;
    }

    static Border Metric(string heading, UIElement value, string explanation,
        Microsoft.UI.Xaml.Media.Brush color)
    {
        var body = new StackPanel { Spacing = 10 };
        var title = VisualTheme.Text(heading, 10, true);
        title.Foreground = color;
        body.Children.Add(title);
        body.Children.Add(value);
        var card = VisualTheme.Card(body, 16);
        card.BorderBrush = color;
        card.BorderThickness = new Thickness(1, 3, 1, 1);
        card.MinHeight = 114;
        ToolTipService.SetToolTip(card, HelpText(explanation));
        return card;
    }
}
