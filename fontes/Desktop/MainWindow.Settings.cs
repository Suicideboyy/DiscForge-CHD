using Microsoft.UI.Xaml.Automation;
using Windows.Storage.Pickers;

sealed partial class MainWindow
{
    // InfoBar carries an icon and text, so the warning never depends on colour alone.
    readonly InfoBar legacyWarning = new()
    {
        Severity = InfoBarSeverity.Warning,
        IsClosable = false,
        IsOpen = false
    };

    /// <summary>Groups common fields and keeps technical choices in one expandable section.</summary>
    void BuildSettings()
    {
        BuildCodecChoices(cdCodecs, cdCodecChoices, new[]
        {
            ("cdlz", "LZMA: favors size"), ("cdzs", "Zstandard: balanced"),
            ("cdzl", "zlib: general purpose"), ("cdfl", "FLAC: CD audio")
        });
        BuildCodecChoices(dvdCodecs, dvdCodecChoices, new[]
        {
            ("lzma", "LZMA: favors size"), ("zstd", "Zstandard: balanced"),
            ("zlib", "zlib: general purpose"), ("flac", "FLAC: audio"),
            ("huff", "Huffman: repeated patterns")
        });
        var library = VisualTheme.Section("01", "Library");
        library.Children.Add(Field("Input", PathRow(input, "Input"), OptionHelp.Input));
        library.Children.Add(Field("Output", PathRow(output, "Output"), OptionHelp.Output));
        var libraryCard = VisualTheme.Card(library);

        var encoder = VisualTheme.Section("02", "Conversion settings");
        encoder.Children.Add(VisualTheme.Pair(
            Field("Platform", platform, OptionHelp.Platform),
            Field("Threads · automatic", threads, OptionHelp.Threads)));
        var tuning = new StackPanel { Spacing = 14 };
        tuning.Children.Add(VisualTheme.Pair(
            Field("CD hunk · bytes", cdHunk, OptionHelp.CdHunk),
            Field("DVD hunk · bytes", dvdHunk, OptionHelp.DvdHunk)));
        tuning.Children.Add(Field("Codecs CD", cdCodecs, OptionHelp.CdCodecs));
        tuning.Children.Add(Field("Codecs DVD", dvdCodecs, OptionHelp.DvdCodecs));
        advanced.Content = tuning;
        advanced.HorizontalAlignment = HorizontalAlignment.Stretch;
        advanced.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        encoder.Children.Add(advanced);
        encoder.Children.Add(WithHelp(legacyCompatibility, "Compatibility with older devices", OptionHelp.LegacyCompatibility));
        legacyWarning.Message = OptionHelp.LegacyWarning;
        AutomationProperties.SetName(legacyWarning, OptionHelp.LegacyWarning);
        encoder.Children.Add(legacyWarning);
        encoder.Children.Add(WithHelp(autoDetect, "Auto-detect system", OptionHelp.AutoDetect));
        encoder.Children.Add(WithHelp(online, "Game lookup", OptionHelp.Lookup));
        encoder.Children.Add(WithHelp(delete, "Remove original", OptionHelp.Delete));
        var encoderCard = VisualTheme.Card(encoder);
        var panels = VisualTheme.Pair(libraryCard, encoderCard);
        panels.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panels.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panels.RowSpacing = VisualTheme.GapL;
        settingsPanels = panels;
        settingsCards = new FrameworkElement[] { libraryCard, encoderCard };
        // Second, narrower breakpoint: the settings cards themselves reflow, with a
        // band around the threshold so the reflow cannot feed itself.
        panels.SizeChanged += (_, _) =>
        {
            if (lastStackedCards.HasValue &&
                Math.Abs(panels.ActualWidth - SettingsStackWidth) < Hysteresis) return;
            bool narrow = panels.ActualWidth < SettingsStackWidth;
            if (lastStackedCards == narrow) return;
            lastStackedCards = narrow;
            Grid.SetColumnSpan(libraryCard, narrow ? 2 : 1);
            Grid.SetColumn(encoderCard, narrow ? 0 : 1);
            Grid.SetRow(encoderCard, narrow ? 1 : 0);
            Grid.SetColumnSpan(encoderCard, narrow ? 2 : 1);
        };
        settings.Children.Add(panels);
    }

    // Preserve the displayed codec order and enforce the four-codec limit.
    static void BuildCodecChoices(StackPanel panel, List<CheckBox> choices,
        IEnumerable<(string Name, string Description)> codecs)
    {
        foreach (var (name, description) in codecs)
        {
            // The label wraps: a longer translation or a larger text size must
            // wrap rather than run off the card with no scrollbar to reveal it.
            string caption = name + " — " + Localization.T(description);
            var choice = new CheckBox
            {
                Content = new TextBlock { Text = caption, TextWrapping = TextWrapping.Wrap },
                Tag = name,
                IsChecked = name != "huff"
            };
            ToolTipService.SetToolTip(choice, Localization.T(description));
            // Wrapped content is not announced on its own, so the choice is named.
            AutomationProperties.SetName(choice, caption);
            choice.Checked += (_, _) =>
            {
                if (choices.Count(c => c.IsChecked == true) > 4)
                    choice.IsChecked = false;
            };
            choices.Add(choice);
            panel.Children.Add(choice);
        }
    }

    static string SelectedCodecs(List<CheckBox> choices) =>
        string.Join(',', choices.Where(c => c.IsChecked == true).Select(c => (string)c.Tag));

    FrameworkElement Field(string label, FrameworkElement control, string explanation)
    {
        var field = new StackPanel { Spacing = 6 };
        // The label reads as secondary so the entered value stays the focus.
        var caption = VisualTheme.Meta(Localization.T(label));
        caption.FontSize = 12;
        caption.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        field.Children.Add(WithHelp(caption, label, explanation));
        control.HorizontalAlignment = HorizontalAlignment.Stretch;
        ToolTipService.SetToolTip(control, HelpText(explanation));
        NameEditor(control, label);
        AutomationProperties.SetHelpText(control, explanation);
        field.Children.Add(control);
        return field;
    }

    /// <summary>
    /// A field may hand Field() a decorated row (a path box plus its Browse button).
    /// A row is not focusable, so only the editor inside it is named; naming the row
    /// too would announce the same text twice in the accessibility tree.
    /// </summary>
    static void NameEditor(FrameworkElement control, string label)
    {
        string name = Localization.T(label);
        if (control is Panel panel)
        {
            foreach (var child in panel.Children.OfType<FrameworkElement>())
                if (child is TextBox or ComboBox or NumberBox)
                    AutomationProperties.SetName(child, name);
            return;
        }
        AutomationProperties.SetName(control, name);
    }

    /// <summary>Help opens by mouse, touch or keyboard without relying on hover.</summary>
    Grid WithHelp(FrameworkElement control, string label, string explanation)
    {
        // Routed through the theme so the chip keeps a native surface in high contrast.
        var help = new Button
        {
            Content = "?", Width = 28, Height = 28, Padding = new Thickness(0),
            CornerRadius = new CornerRadius(14),
            Flyout = new Flyout { Content = HelpText(explanation) }
        };
        VisualTheme.Quiet(help);
        AutomationProperties.SetName(help, (Localization.IsPortuguese ? "Ajuda: " : "Help: ") + Localization.T(label));
        ToolTipService.SetToolTip(help, Localization.IsPortuguese ? "Explicar esta opção" : "Explain this option");
        helpButtons.Add(help);
        var row = VisualTheme.Pair(control, help);
        row.ColumnDefinitions[1].Width = GridLength.Auto;
        return row;
    }

    static TextBlock HelpText(string text) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 340, FontSize = 13
    };

    Grid PathRow(TextBox box, string label, bool file = false)
    {
        // Same height as the editor beside it, so every input row lines up.
        var choose = new Button
        {
            Content = Localization.T("Browse…"), MinHeight = VisualTheme.ControlSmall
        };
        AutomationProperties.SetName(choose,
            Localization.T("Browse…") + " — " + Localization.T(label));
        choose.Click += async (_, _) =>
        {
            try
            {
                if (file)
                {
                    var picker = new FileOpenPicker();
                    WinRT.Interop.InitializeWithWindow.Initialize(picker,
                        WinRT.Interop.WindowNative.GetWindowHandle(this));
                    picker.FileTypeFilter.Add(".exe");
                    picker.FileTypeFilter.Add(".dll");
                    var selected = await picker.PickSingleFileAsync();
                    if (selected != null) box.Text = selected.Path;
                }
                else
                {
                    var picker = new FolderPicker();
                    WinRT.Interop.InitializeWithWindow.Initialize(picker,
                        WinRT.Interop.WindowNative.GetWindowHandle(this));
                    picker.FileTypeFilter.Add("*");
                    var folder = await picker.PickSingleFolderAsync();
                    if (folder != null) box.Text = folder.Path;
                }
            }
            catch (Exception ex) { Append("Browse: " + ex.Message); }
        };
        var row = VisualTheme.Pair(box, choose);
        row.ColumnDefinitions[1].Width = GridLength.Auto;
        return row;
    }

    void UpdatePlatform()
    {
        bool ps2 = (string)platform.SelectedItem == "PS2";
        bool modern = legacyCompatibility.IsChecked != true;
        legacyWarning.IsOpen = !modern;
        cdHunk.IsEnabled = modern;
        dvdHunk.IsEnabled = modern && (ps2 || autoDetect.IsChecked == true);
        online.IsEnabled = delete.IsEnabled = ps2 || autoDetect.IsChecked == true;
        foreach (var choice in cdCodecChoices) choice.IsEnabled = modern;
        foreach (var choice in dvdCodecChoices) choice.IsEnabled = modern && (ps2 || autoDetect.IsChecked == true);
    }

    void SetSettingsEnabled(bool enabled)
    {
        start.IsEnabled = settingsHost.IsEnabled = enabled;
        language.IsEnabled = resetDefaults.IsEnabled = enabled;
        if (toolSettingsPage != null) toolSettingsPage.IsEnabled = enabled;
        chdmanPath.IsEnabled = enabled;
        if (enabled) UpdatePlatform();
    }
}
