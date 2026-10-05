using Microsoft.UI.Xaml.Automation;

sealed partial class MainWindow
{
    /// <summary>Wires window lifecycle and avoids closing during an active input.</summary>
    void ConnectEvents()
    {
        start.Click += async (_, _) => await StartAsync();
        stop.Click += (_, _) => RequestStop();
        stopNow.Click += (_, _) => RequestStopNow();
        // openOutput is a field but its content is rebuilt on every language change,
        // so its handler is wired once here and never inside BuildProgress.
        openOutput.Click += (_, _) =>
        {
            try
            {
                if (Directory.Exists(output.Text))
                    Process.Start(new ProcessStartInfo(output.Text) { UseShellExecute = true });
            }
            catch (Exception ex) { Append("Open output: " + ex.Message); }
        };
        // Subscribed once here, never inside a rebuild, so a language change
        // cannot stack duplicate handlers on the navigation shell.
        navigation.SelectionChanged += (_, _) => OnNavigation();
        input.TextChanged += (_, _) => FollowInputFolder();
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
            RebuildNavigation(1);
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
            telemetry.Dispose();
            game.Dispose();
        };
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += async (_, _) => await RefreshTelemetry();
        timer.Start();
        UpdatePlatform();
    }

    /// <summary>Shows the page behind the selected item without rebuilding it.</summary>
    void OnNavigation()
    {
        if (navigation.SelectedItem is not NavigationViewItem item) return;
        for (int i = 0; i < navigation.MenuItems.Count; i++)
        {
            if (!ReferenceEquals(navigation.MenuItems[i], item)) continue;
            if (i < pages.Length) navigation.Content = pages[i];
            return;
        }
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
        // actions is a field; DetachTree clears it on a language rebuild.
        actions.Orientation = Orientation.Horizontal;
        if (VisualTheme.AnimationsEnabled)
            actions.ChildrenTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection
            {
                new Microsoft.UI.Xaml.Media.Animation.AddDeleteThemeTransition()
            };
        // One committing action, one supporting action, one destructive action.
        VisualTheme.Primary(start);
        VisualTheme.Secondary(stop);
        VisualTheme.Destructive(stopNow);
        VisualTheme.Subtle(openOutput);
        start.AccessKey = "S";
        // Every explanation comes from OptionHelp, which is localized in both
        // languages, so a tooltip and its screen reader text never disagree.
        Describe(start, Symbol.Play, Localization.T("Start conversion"), OptionHelp.Start);
        Describe(stop, Symbol.Pause, Localization.T("Stop after current"), OptionHelp.Stop);
        Describe(stopNow, Symbol.Stop, Localization.T("Stop now"), OptionHelp.StopNow);
        Describe(openOutput, Symbol.OpenFile, Localization.T("Open output"), OptionHelp.OpenOutput);
        var open = openOutput;
        actions.Children.Add(start);
        actions.Children.Add(stop);
        actions.Children.Add(stopNow);
        actions.Children.Add(open);
        body.Children.Add(actions);
        // Screen readers get the label from the name, not from the icon glyph.
        AutomationProperties.SetName(stage, Localization.T("Current input progress"));
        AutomationProperties.SetName(batch, Localization.T("Queue progress"));
        AutomationProperties.SetName(log, Localization.T("Activity log"));
        body.Children.Add(new Expander
        {
            Header = Localization.T("Activity log"), Content = log,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        });
        return VisualTheme.Card(body);
    }

    // One place so every command carries a tooltip, an accessible name and help text.
    static void Describe(Button button, Symbol symbol, string label, string explanation)
    {
        button.Content = ActionLabel(symbol, label, button.Foreground);
        ToolTipService.SetToolTip(button, HelpText(explanation));
        AutomationProperties.SetName(button, label);
        AutomationProperties.SetHelpText(button, explanation);
    }

    static StackPanel ActionLabel(Symbol symbol, string label, Microsoft.UI.Xaml.Media.Brush foreground)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        row.Children.Add(new SymbolIcon(symbol) { Foreground = foreground });
        var caption = VisualTheme.Text(label, 13, true);
        caption.Foreground = foreground;
        row.Children.Add(caption);
        return row;
    }
}
