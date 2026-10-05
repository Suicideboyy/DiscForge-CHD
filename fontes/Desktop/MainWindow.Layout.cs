using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;

sealed partial class MainWindow : Window
{
    // Height of the drag region that hosts the app name. Caption buttons stay
    // system-drawn in the top right, so no interactive content lives there.
    const double TitleBarHeight = 32;
    // The navigation pane is expanded above this width and collapses to the icon
    // rail below it, handing its width back to the Bento.
    const double PaneExpandWidth = 1100;
    // The Bento stacks the game card under the work column below this page width.
    const double StackWidth = 900;
    // Inside the work column the two settings cards stack below this width.
    const double SettingsStackWidth = 740;
    // Rows of equal cards restack into one column below these page widths, so a
    // card never has to squeeze the value it holds.
    const double MetricStackWidth = 560, LegendStackWidth = 460;
    // A reflow changes the page height, which can toggle the vertical scrollbar and
    // shift the very width that drove it, so a state only flips outside this band.
    const double Hysteresis = 24;
    // The game card hosts a WebView, which needs a definite height to lay out.
    const double GameCardHeight = 810;
    const double GameCardHeightStacked = 640;

    public MainWindow(string[] args)
    {
        Title = AppInfo.DisplayName;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1380, 1000));
        string icon = Path.Combine(AppContext.BaseDirectory, "Assets", "discforge.ico");
        if (File.Exists(icon)) AppWindow.SetIcon(icon);
        LoadPreferences();
        BuildShell();
        ConnectEvents();
        if (args.Length == 2 && args[0] == "--ui-smoke") RunSmoke(args[1]);
    }

    void BuildShell()
    {
        // Field defaults carry English text, so they must follow the saved language
        // before any page reads them.
        ApplyControlLabels();
        // Nothing has been laid out yet, so ActualWidth is still zero; the window
        // size already requested above is the only honest source for the first
        // pane state, otherwise the pane snaps open after the first layout pass.
        lastExpandedPane = AppWindow.Size.Width >= PaneExpandWidth;
        navigation.IsPaneOpen = lastExpandedPane;
        micaActive = TryEnableMica();
        // Mica only reads through a transparent root, so the canvas is dropped
        // when the backdrop is live and kept as the fallback when it is not.
        var workspace = new Grid
        {
            Background = micaActive ? null : VisualTheme.Canvas,
            RequestedTheme = ElementTheme.Dark
        };
        // Auto so the drag region grows instead of clipping its label at large
        // text sizes; it measures exactly TitleBarHeight at the default one.
        workspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        workspace.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        workspace.Children.Add(BuildTitleBar());
        Grid.SetRow(navigation, 1);
        navigation.Background = null;
        navigation.HorizontalAlignment = HorizontalAlignment.Stretch;
        navigation.VerticalAlignment = VerticalAlignment.Stretch;
        workspace.Children.Add(navigation);
        BuildNavigation();
        // NavigationView keeps an open pane open at any window width, so the
        // responsive decision is made here: expanded above the threshold, icon rail
        // below it. Subscribed once, never inside a rebuild.
        navigation.SizeChanged += (_, _) =>
        {
            bool expanded = navigation.ActualWidth >= PaneExpandWidth;
            if (expanded == lastExpandedPane) return;
            lastExpandedPane = expanded;
            navigation.IsPaneOpen = expanded;
        };
        Content = workspace;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
    }

    // Mica is the window backdrop; the cards above it supply the tonal depth.
    // Returns false so the caller can fall back to the solid canvas.
    bool TryEnableMica()
    {
        try
        {
            // High contrast suppresses the backdrop, so the solid canvas is both
            // correct and cheaper there.
            if (VisualTheme.HighContrast) return false;
            if (!MicaController.IsSupported()) return false;
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
            return true;
        }
        catch (Exception) { return false; }
    }

    Grid BuildTitleBar()
    {
        titleBar = new Grid { MinHeight = TitleBarHeight };
        var name = VisualTheme.Text(AppInfo.Name, 12, true);
        name.Foreground = VisualTheme.Muted;
        name.VerticalAlignment = VerticalAlignment.Center;
        // Clear of the caption buttons on the right.
        name.Margin = new Thickness(16, 0, 200, 0);
        titleBar.Children.Add(name);
        return titleBar;
    }

    void BuildNavigation()
    {
        navigation.MenuItems.Add(new NavigationViewItem
        {
            Content = Localization.T("Conversion"),
            Icon = new SymbolIcon { Symbol = Symbol.Play },
            Tag = "conversion"
        });
        navigation.MenuItems.Add(new NavigationViewItem
        {
            Content = Localization.T("Settings"),
            Icon = new SymbolIcon { Symbol = Symbol.Setting },
            Tag = "settings"
        });
        navigation.MenuItems.Add(new NavigationViewItem
        {
            Content = Localization.T("About"),
            Icon = new SymbolIcon { Symbol = Symbol.Help },
            Tag = "about"
        });
        pages = new[]
        {
            BuildConversion(), BuildToolSettings(), BuildAbout()
        };
        ShowPage(0);
    }

    /// <summary>Swaps the visible page without rebuilding it, so state and focus survive.</summary>
    void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Length) return;
        navigation.Content = pages[index];
        navigation.SelectedItem = navigation.MenuItems[index];
    }

    /// <summary>Rebuilds only UI chrome when the user changes language.</summary>
    void RebuildNavigation(int selected = 0)
    {
        navigation.Content = null;
        foreach (var page in pages) DetachTree(page);
        navigation.MenuItems.Clear();
        settings.Children.Clear();
        cdCodecs.Children.Clear();
        dvdCodecs.Children.Clear();
        cdCodecChoices.Clear();
        dvdCodecChoices.Clear();
        helpButtons.Clear();
        // Every grid guarded by a reflow latch is about to be thrown away and
        // rebuilt in its default arrangement. A stale latch would make the first
        // SizeChanged of the new grid return early and leave it unstacked.
        lastStacked = lastStackedTiles = lastStackedCaptions = lastStackedCards = null;
        lastExpandedPane = navigation.ActualWidth >= PaneExpandWidth;
        ApplyControlLabels();
        BuildNavigation();
        ShowPage(selected);
        game.RefreshLanguage();
    }

    // Shared controls must leave their old parents before language rebuilds the pages.
    void DetachTree(UIElement element)
    {
        if (element == null || ReferenceEquals(element, game) || ReferenceEquals(element, telemetry)) return;
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

    UIElement BuildConversion()
    {
        var page = new StackPanel { Spacing = VisualTheme.GapXL, Padding = new Thickness(24) };
        if (VisualTheme.AnimationsEnabled)
            page.ChildrenTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection
            {
                new Microsoft.UI.Xaml.Media.Animation.EntranceThemeTransition()
            };
        page.Children.Add(BuildPageHeading(Localization.T("Conversion"),
            Localization.T("Your collection. Less space. Every experience.")));

        // Both columns are proportional, so the game card keeps a definite width
        // for its WebView instead of sizing to its content.
        var columns = new Grid { ColumnSpacing = VisualTheme.GapXL, RowSpacing = VisualTheme.GapXL };
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) });
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        columns.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        columns.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Work column: settings and the running operation.
        var work = new StackPanel { Spacing = VisualTheme.GapL };
        BuildSettings();
        settingsHost.Content = settings;
        work.Children.Add(settingsHost);
        work.Children.Add(BuildProgress());
        columns.Children.Add(work);

        var details = VisualTheme.Card(game, 0);
        details.Height = GameCardHeight;
        details.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(details, 1);
        columns.Children.Add(details);
        gameCard = details;
        AutomationProperties.SetName(game, Localization.T("Current game"));

        // The four commands are measured against the work column they actually sit
        // in, never against an ancestor, so they cannot overflow the card edge.
        work.SizeChanged += (_, _) =>
        {
            bool stacked = NaturalWidth(actions) + VisualTheme.GapM > actions.ActualWidth;
            actions.Orientation = stacked ? Orientation.Vertical : Orientation.Horizontal;
            foreach (var command in actions.Children.OfType<FrameworkElement>())
                command.HorizontalAlignment =
                    stacked ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
        };

        page.Children.Add(BuildMetrics());
        page.Children.Add(columns);
        page.Children.Add(BuildTelemetry());

        // Single place where the Bento itself reflows. Each contained row drives its
        // own reflow from its own measured width: a child read from an ancestor's
        // SizeChanged still reports the previous layout.
        page.SizeChanged += (_, _) =>
        {
            if (lastStacked.HasValue && Math.Abs(page.ActualWidth - StackWidth) < Hysteresis) return;
            bool stacked = page.ActualWidth < StackWidth;
            if (lastStacked == stacked) return;
            lastStacked = stacked;
            Grid.SetColumnSpan(work, stacked ? 2 : 1);
            Grid.SetRow(details, stacked ? 1 : 0);
            Grid.SetColumn(details, stacked ? 0 : 1);
            Grid.SetColumnSpan(details, stacked ? 2 : 1);
            details.Height = stacked ? GameCardHeightStacked : GameCardHeight;
            // The second row is empty when unstacked, so its spacing would add
            // dead height under the Bento.
            columns.RowSpacing = stacked ? VisualTheme.GapXL : 0;
        };

        return new ScrollViewer
        {
            Content = page,
            Background = null,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    /// <summary>Width the children need side by side, independent of how they are
    /// currently arranged.</summary>
    static double NaturalWidth(StackPanel row) => row.Children.OfType<FrameworkElement>()
        .Sum(child => child.DesiredSize.Width)
        + row.Spacing * Math.Max(0, row.Children.Count - 1);

    /// <summary>Equal-width cards in one row restack into a single column when the
    /// row can no longer hold their content. Driven only by measured width, with a
    /// band around the threshold so it cannot oscillate.</summary>
    static void StackRow(Grid row, IReadOnlyList<FrameworkElement> cells, double threshold,
        ref bool? lastNarrow)
    {
        if (row == null) return;
        if (lastNarrow.HasValue && Math.Abs(row.ActualWidth - threshold) < Hysteresis) return;
        bool narrow = row.ActualWidth < threshold;
        if (lastNarrow == narrow) return;
        lastNarrow = narrow;
        row.RowSpacing = VisualTheme.GapM;
        row.ColumnDefinitions.Clear();
        row.RowDefinitions.Clear();
        int columns = narrow ? 1 : cells.Count;
        for (int c = 0; c < columns; c++) row.ColumnDefinitions.Add(new ColumnDefinition());
        for (int r = 0; r < (narrow ? cells.Count : 1); r++)
            row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int i = 0; i < cells.Count; i++)
        {
            Grid.SetColumn(cells[i], narrow ? 0 : i);
            Grid.SetRow(cells[i], narrow ? i : 0);
        }
    }

    // Compact page heading. The window chrome already carries the product identity.
    static UIElement BuildPageHeading(string title, string subtitle)
    {
        var body = new StackPanel { Spacing = 2 };
        body.Children.Add(VisualTheme.PageTitle(title));
        body.Children.Add(VisualTheme.Meta(subtitle));
        return body;
    }

    UIElement BuildMetrics()
    {
        var row = new Grid { ColumnSpacing = VisualTheme.GapM };
        // Elapsed and CPU are the primary readouts; disk is supporting metadata.
        disk.Foreground = VisualTheme.Muted;
        var cards = new[]
        {
            Metric(Localization.T("CURRENT INPUT"), elapsed, OptionHelp.Time, VisualTheme.Accent),
            Metric(Localization.T("CPU · APP"), cpu, OptionHelp.Cpu, VisualTheme.Teal),
            Metric(Localization.T("DISK · APP"), disk, OptionHelp.Disk, VisualTheme.Warning)
        };
        foreach (var card in cards)
        {
            row.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(card, row.Children.Count);
            row.Children.Add(card);
        }
        for (int i = 0; i < cards.Length; i++) metricTiles[i] = cards[i];
        metricRow = row;
        // Each row drives its own reflow: reading a child's width from an ancestor's
        // SizeChanged returns the previous layout, because the child has not been
        // re-measured yet.
        row.SizeChanged += (_, _) =>
            StackRow(row, metricTiles, MetricStackWidth, ref lastStackedTiles);
        return row;
    }

    // Single Plotly strip (CPU | disk | stage progress) across the full page width.
    // Text values stay in the stat tiles above; the WebView is a decorative layer.
    UIElement BuildTelemetry()
    {
        var body = new StackPanel { Spacing = VisualTheme.GapS };
        body.Children.Add(VisualTheme.Label(
            Localization.T("Performance") + " · " + Localization.T("last 90 s")));
        // One caption per chart, above the strip it describes.
        var captions = new Grid { ColumnSpacing = VisualTheme.GapM, RowSpacing = VisualTheme.GapXS };
        var entries = new (string Text, Microsoft.UI.Xaml.Media.Brush Color)[]
        {
            (Localization.T("CPU"), VisualTheme.Teal),
            (Localization.T("Disk I/O"), VisualTheme.Warning),
            (Localization.T("Stage progress"), VisualTheme.Accent)
        };
        for (int i = 0; i < entries.Length; i++)
        {
            captions.ColumnDefinitions.Add(new ColumnDefinition());
            var caption = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = VisualTheme.GapXS,
                VerticalAlignment = VerticalAlignment.Center
            };
            caption.Children.Add(new Border
            {
                Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
                Background = entries[i].Color
            });
            var text = VisualTheme.Meta(entries[i].Text);
            if (i == 2) stageLegend = text;
            caption.Children.Add(text);
            Grid.SetColumn(caption, i);
            legendItems[i] = caption;
            captions.Children.Add(caption);
        }
        body.Children.Add(captions);
        legend = captions;
        captions.SizeChanged += (_, _) =>
            StackRow(captions, legendItems, LegendStackWidth, ref lastStackedCaptions);
        telemetry.HorizontalAlignment = HorizontalAlignment.Stretch;
        telemetry.Height = 68;
        body.Children.Add(telemetry);
        return VisualTheme.Card(body, 16);
    }

    static Border Metric(string heading, UIElement value, string explanation,
        Microsoft.UI.Xaml.Media.Brush color)
    {
        var body = new StackPanel { Spacing = VisualTheme.GapM };
        var title = VisualTheme.Label(heading);
        title.Foreground = color;
        body.Children.Add(title);
        body.Children.Add(value);
        var card = VisualTheme.Card(body, 16);
        card.MinHeight = 104;
        card.VerticalAlignment = VerticalAlignment.Stretch;
        ToolTipService.SetToolTip(card, HelpText(explanation));
        return card;
    }
}
