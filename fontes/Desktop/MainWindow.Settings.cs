using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

sealed partial class MainWindow
{
    /// <summary>Agrupa os campos comuns e mantém os ajustes técnicos em uma seção expansível.</summary>
    void BuildSettings()
    {
        var library = VisualTheme.Section("01", "Biblioteca");
        library.Children.Add(Field("Entrada", PathRow(input), OptionHelp.Input));
        library.Children.Add(Field("Saída", PathRow(output), OptionHelp.Output));
        var libraryCard = VisualTheme.Card(library);

        var encoder = VisualTheme.Section("02", "Conversão");
        encoder.Children.Add(VisualTheme.Pair(
            Field("Plataforma", platform, OptionHelp.Platform),
            Field("Threads · automático", threads, OptionHelp.Threads)));
        var tuning = new StackPanel { Spacing = 14 };
        tuning.Children.Add(VisualTheme.Pair(
            Field("Bloco CD · bytes", cdHunk, OptionHelp.CdHunk),
            Field("Bloco DVD · bytes", dvdHunk, OptionHelp.DvdHunk)));
        tuning.Children.Add(Field("Codecs CD", cdCodecs, OptionHelp.CdCodecs));
        tuning.Children.Add(Field("Codecs DVD", dvdCodecs, OptionHelp.DvdCodecs));
        advanced.Content = tuning;
        advanced.HorizontalAlignment = HorizontalAlignment.Stretch;
        advanced.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        encoder.Children.Add(advanced);
        encoder.Children.Add(WithHelp(online, "Consulta do jogo", OptionHelp.Lookup));
        encoder.Children.Add(WithHelp(delete, "Remoção do original", OptionHelp.Delete));
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

    /// <summary>A mesma ajuda pode ser aberta por mouse, toque ou teclado, sem depender de hover.</summary>
    Grid WithHelp(FrameworkElement control, string label, string explanation)
    {
        var help = new Button
        {
            Content = "?", Width = 28, Height = 28, Padding = new Thickness(0),
            Background = VisualTheme.Brush(240, 238, 254), Foreground = VisualTheme.Accent,
            CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(0),
            Flyout = new Flyout { Content = HelpText(explanation) }
        };
        AutomationProperties.SetName(help, "Ajuda: " + label);
        ToolTipService.SetToolTip(help, "Entenda esta opção");
        helpButtons.Add(help);
        var row = VisualTheme.Pair(control, help);
        row.ColumnDefinitions[1].Width = GridLength.Auto;
        return row;
    }

    static TextBlock HelpText(string text) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 340, FontSize = 13
    };

    Grid PathRow(TextBox box)
    {
        var choose = new Button { Content = "Escolher…", MinHeight = 34 };
        choose.Click += async (_, _) =>
        {
            try
            {
                var picker = new FolderPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
                picker.FileTypeFilter.Add("*");
                var folder = await picker.PickSingleFolderAsync();
                if (folder != null) box.Text = folder.Path;
            }
            catch (Exception ex) { Append("Seleção de pasta: " + ex.Message); }
        };
        var row = VisualTheme.Pair(box, choose);
        row.ColumnDefinitions[1].Width = GridLength.Auto;
        return row;
    }

    void UpdatePlatform()
    {
        bool ps2 = (string)platform.SelectedItem == "PS2";
        dvdHunk.IsEnabled = dvdCodecs.IsEnabled = online.IsEnabled = delete.IsEnabled = ps2;
    }

    void SetSettingsEnabled(bool enabled)
    {
        start.IsEnabled = settingsHost.IsEnabled = enabled;
        if (enabled) UpdatePlatform();
    }
}
