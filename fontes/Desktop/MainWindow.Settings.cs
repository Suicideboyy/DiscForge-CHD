using System;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

sealed partial class MainWindow
{
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
        library.Children.Add(Field("Input", PathRow(input), OptionHelp.Input));
        library.Children.Add(Field("Output", PathRow(output), OptionHelp.Output));
        var libraryCard = VisualTheme.Card(library);

        var encoder = VisualTheme.Section("02", "Conversion");
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
        encoder.Children.Add(WithHelp(online, "Game lookup", OptionHelp.Lookup));
        encoder.Children.Add(WithHelp(delete, "Remove original", OptionHelp.Delete));
        var encoderCard = VisualTheme.Card(encoder);
        var panels = VisualTheme.Pair(libraryCard, encoderCard);
        panels.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panels.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panels.RowSpacing = 16;
        panels.SizeChanged += (_, _) =>
        {
            bool narrow = panels.ActualWidth < 740;
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
            var choice = new CheckBox
            {
                Content = name + " — " + description,
                Tag = name,
                IsChecked = name != "huff"
            };
            ToolTipService.SetToolTip(choice, description);
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
        field.Children.Add(WithHelp(VisualTheme.Text(label, 12, true), label, explanation));
        control.HorizontalAlignment = HorizontalAlignment.Stretch;
        ToolTipService.SetToolTip(control, HelpText(explanation));
        AutomationProperties.SetName(control, label);
        AutomationProperties.SetHelpText(control, explanation);
        field.Children.Add(control);
        return field;
    }

    /// <summary>Help opens by mouse, touch or keyboard without relying on hover.</summary>
    Grid WithHelp(FrameworkElement control, string label, string explanation)
    {
        var help = new Button
        {
            Content = "?", Width = 28, Height = 28, Padding = new Thickness(0),
            Background = VisualTheme.Brush(240, 238, 254), Foreground = VisualTheme.Accent,
            CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(0),
            Flyout = new Flyout { Content = HelpText(explanation) }
        };
        AutomationProperties.SetName(help, "Help: " + label);
        ToolTipService.SetToolTip(help, "Explain this option");
        helpButtons.Add(help);
        var row = VisualTheme.Pair(control, help);
        row.ColumnDefinitions[1].Width = GridLength.Auto;
        return row;
    }

    static TextBlock HelpText(string text) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 340, FontSize = 13
    };

    Grid PathRow(TextBox box, bool file = false)
    {
        var choose = new Button { Content = "Browse…", MinHeight = 34 };
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
        dvdHunk.IsEnabled = online.IsEnabled = delete.IsEnabled = ps2;
        foreach (var choice in dvdCodecChoices) choice.IsEnabled = ps2;
    }

    void SetSettingsEnabled(bool enabled)
    {
        start.IsEnabled = settingsHost.IsEnabled = enabled;
        chdmanPath.IsEnabled = sevenZipExePath.IsEnabled = sevenZipDllPath.IsEnabled = enabled;
        if (enabled) UpdatePlatform();
    }
}
