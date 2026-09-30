using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

sealed partial class MainWindow
{
    /// <summary>Wires window lifecycle and avoids closing during an active input.</summary>
    void ConnectEvents()
    {
        start.Click += async (_, _) => await StartAsync();
        stop.Click += (_, _) => RequestStop();
        stopNow.Click += (_, _) => RequestStopNow();
        input.TextChanged += (_, _) => FollowInputFolder();
        ramDiskPath.TextChanged += (_, _) => RefreshRamAvailability();
        platform.SelectionChanged += (_, _) => { UpdatePlatform(); game.Reset(); };
        autoDetect.Checked += (_, _) => UpdatePlatform();
        autoDetect.Unchecked += (_, _) => UpdatePlatform();
        legacyCompatibility.Checked += (_, _) => UpdatePlatform();
        legacyCompatibility.Unchecked += (_, _) => UpdatePlatform();
        resetDefaults.Click += (_, _) => RestoreDefaults();
        language.SelectionChanged += (_, _) =>
        {
            if (running) return;
            bool portuguese = language.SelectedIndex == 1;
            if (Localization.IsPortuguese == portuguese) return;
            Localization.SetLanguage(portuguese ? "pt-BR" : "en");
            BuildTabs(1);
            SavePreferences();
        };
        AppWindow.Closing += (_, args) =>
        {
            if (!running) return;
            args.Cancel = true;
            RequestStop();
            status.Text = Localization.T("Wait for the current input before closing.");
        };
        Closed += (_, _) =>
        {
            closed = true;
            SavePreferences();
            timer.Stop();
            performance.Dispose();
            game.Dispose();
        };
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += async (_, _) => await RefreshTelemetry();
        timer.Start();
        UpdatePlatform();
        RefreshRamAvailability();
    }

    UIElement BuildProgress()
    {
        var body = VisualTheme.Section("03", "Processing");
        stage.Foreground = VisualTheme.Accent;
        batch.Foreground = VisualTheme.Teal;
        body.Children.Add(status);
        body.Children.Add(stage);
        body.Children.Add(batchStatus);
        body.Children.Add(batch);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.ChildrenTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection
        {
            new Microsoft.UI.Xaml.Media.Animation.AddDeleteThemeTransition()
        };
        start.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        start.Background = VisualTheme.Accent;
        start.Content = ActionLabel(Symbol.Play, Localization.T("Start conversion"));
        stop.Content = ActionLabel(Symbol.Pause, Localization.T("Stop after current"));
        stopNow.Content = ActionLabel(Symbol.Stop, Localization.T("Stop now"));
        ToolTipService.SetToolTip(start, HelpText("Validates options and starts the queue. Every CHD is verified before saving."));
        ToolTipService.SetToolTip(stop, HelpText(OptionHelp.Stop));
        var open = new Button { Content = Localization.T("Open output"), MinHeight = 42 };
        open.Content = ActionLabel(Symbol.OpenFile, Localization.T("Open output"));
        ToolTipService.SetToolTip(open, Localization.IsPortuguese ? "Abre a pasta de saída no Explorador de Arquivos." : "Opens the output folder in File Explorer.");
        open.Click += (_, _) =>
        {
            try
            {
                if (Directory.Exists(output.Text))
                    Process.Start(new ProcessStartInfo(output.Text) { UseShellExecute = true });
            }
            catch (Exception ex) { Append("Open output: " + ex.Message); }
        };
        actions.Children.Add(start);
        actions.Children.Add(stop);
        stopNow.Background = VisualTheme.Brush(243, 213, 219);
        ToolTipService.SetToolTip(stopNow, HelpText(OptionHelp.StopNow));
        actions.Children.Add(stopNow);
        actions.Children.Add(open);
        body.Children.Add(actions);
        body.Children.Add(new Expander
        {
            Header = Localization.T("Activity log"), Content = log,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        });
        return VisualTheme.Card(body);
    }

    static StackPanel ActionLabel(Symbol symbol, string label)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        row.Children.Add(new SymbolIcon(symbol));
        row.Children.Add(VisualTheme.Text(label, 13, true));
        return row;
    }
}
