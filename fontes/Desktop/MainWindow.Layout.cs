using Microsoft.UI.Xaml.Media.Imaging;

sealed partial class MainWindow : Window
{
    public MainWindow(string[] args)
    {
        Title = AppInfo.DisplayName;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1380, 1000));
        string icon = Path.Combine(AppContext.BaseDirectory, "Assets", "discforge.ico");
        if (File.Exists(icon)) AppWindow.SetIcon(icon);
        LoadPreferences();
        BuildTabs();
        // Theme the full client area, including space beyond the selected tab.
        var workspace = new Grid { Background = VisualTheme.Canvas, RequestedTheme = ElementTheme.Dark };
        tabs.HorizontalAlignment = tabs.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        tabs.VerticalAlignment = tabs.VerticalContentAlignment = VerticalAlignment.Stretch;
        workspace.Children.Add(tabs);
        Content = workspace;
        ConnectEvents();
        if (args.Length == 2 && args[0] == "--ui-smoke") RunSmoke(args[1]);
    }

    /// <summary>Rebuilds only UI chrome when the user changes language.</summary>
    void BuildTabs(int selected = 0)
    {
        foreach (var old in tabs.TabItems.OfType<TabViewItem>())
        {
            DetachTree(old.Content as UIElement);
            old.Content = null;
        }
        tabs.TabItems.Clear();
        settings.Children.Clear();
        cdCodecs.Children.Clear();
        dvdCodecs.Children.Clear();
        cdCodecChoices.Clear();
        dvdCodecChoices.Clear();
        helpButtons.Clear();
        ApplyControlLabels();
        tabs.Background = VisualTheme.Canvas;
        tabs.TabItems.Add(new TabViewItem { Header = Localization.T("Conversion"), IsClosable = false,
            IconSource = new SymbolIconSource { Symbol = Symbol.Play }, Content = BuildConversion() });
        tabs.TabItems.Add(new TabViewItem { Header = Localization.T("Settings"), IsClosable = false,
            IconSource = new SymbolIconSource { Symbol = Symbol.Setting }, Content = BuildToolSettings() });
        tabs.TabItems.Add(new TabViewItem { Header = Localization.T("About"), IsClosable = false,
            IconSource = new SymbolIconSource { Symbol = Symbol.Help }, Content = BuildAbout() });
        foreach (var tab in tabs.TabItems.OfType<TabViewItem>())
        {
            tab.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            tab.VerticalContentAlignment = VerticalAlignment.Stretch;
        }
        tabs.SelectedIndex = selected;
        game.RefreshLanguage();
    }

    // Shared controls must leave their old parents before language rebuilds the pages.
    void DetachTree(UIElement element)
    {
        if (element == null || ReferenceEquals(element, game)) return;
        if (element is Panel panel)
        {
            foreach (var child in panel.Children.ToArray()) DetachTree(child);
            panel.Children.Clear();
        }
        else if (element is Border border)
        {
            DetachTree(border.Child);
            border.Child = null;
        }
        else if (element is ScrollViewer scroll)
        {
            DetachTree(scroll.Content as UIElement);
            scroll.Content = null;
        }
        else if (element is ContentControl content)
        {
            DetachTree(content.Content as UIElement);
            content.Content = null;
        }
    }

    /// <summary>Builds the workspace and stacks the game panel in narrow windows.</summary>
    UIElement BuildConversion()
    {
        var page = new StackPanel { Spacing = 20, Padding = new Thickness(24) };
        if (new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
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
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    UIElement BuildHeader()
    {
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        brand.Children.Add(new Image
        {
            Source = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "discforge.png"))),
            Width = 48, Height = 48
        });
        var name = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
        var title = VisualTheme.Text(AppInfo.Name, 27, true);
        title.Foreground = VisualTheme.White;
        name.Children.Add(title);
        var subtitle = VisualTheme.Text(Localization.T("Your collection. Less space. Every experience."), 13);
        subtitle.Foreground = VisualTheme.Muted;
        name.Children.Add(subtitle);
        brand.Children.Add(name);
        var version = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 6 };
        var release = VisualTheme.Text(Localization.T("VERSION ") + AppInfo.Version, 12, true);
        release.Foreground = VisualTheme.Teal;
        var date = VisualTheme.Text(Localization.T("Built on ") + BuildInfo.Date, 12);
        date.Foreground = VisualTheme.Muted;
        version.Children.Add(release);
        version.Children.Add(date);
        var row = VisualTheme.Pair(brand, version);
        row.ColumnDefinitions[1].Width = GridLength.Auto;
        return new Border
        {
            Background = VisualTheme.Navy, CornerRadius = new CornerRadius(16),
            BorderBrush = VisualTheme.Outline, BorderThickness = new Thickness(1),
            Padding = new Thickness(24, 20, 24, 20), Child = row
        };
    }

    UIElement BuildMetrics()
    {
        var row = new Grid { ColumnSpacing = 10 };
        var cards = new[]
        {
            Metric(Localization.T("CURRENT INPUT"), elapsed, OptionHelp.Time, VisualTheme.Accent),
            Metric(Localization.T("CPU · APP"), cpu, OptionHelp.Cpu, VisualTheme.Teal),
            Metric(Localization.T("DISK · APP"), disk, OptionHelp.Disk,
                VisualTheme.HighContrast ? VisualTheme.Ink : VisualTheme.Brush(251, 191, 36))
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
        var title = VisualTheme.Text(heading, 11, true);
        title.Foreground = color;
        body.Children.Add(title);
        body.Children.Add(value);
        var card = VisualTheme.Card(body, 16);
        card.BorderBrush = VisualTheme.Outline;
        card.MinHeight = 104;
        ToolTipService.SetToolTip(card, HelpText(explanation));
        return card;
    }
}
