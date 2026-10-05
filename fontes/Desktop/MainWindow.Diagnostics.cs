using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Automation;

sealed partial class MainWindow
{
    // A control announces itself through an explicit name, plain string content, or
    // an Expander header; anything else reaches a screen reader as an unnamed element.
    static bool Announces(Control control) =>
        !string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)) ||
        (control is ContentControl content && content.Content is string text
            && !string.IsNullOrWhiteSpace(text)) ||
        (control is Expander expander && expander.Header is string header
            && !string.IsNullOrWhiteSpace(header));

    // Screen readers must reach every interactive control by a stable name, and every
    // control that opens help must announce both a name and the explanation.
    void AssertAccessibility(string page)
    {
        foreach (var control in new Control[]
        {
            input, output, platform, threads, cdHunk, dvdHunk, language, chdmanPath,
            log, stage, batch, start, stop, stopNow, openOutput, resetDefaults,
            legacyWarning, autoDetect, online, delete, legacyCompatibility, advanced, aboutChanges
        })
            if (!Announces(control))
                throw new InvalidOperationException(
                    $"{page}: {control.GetType().Name} has no accessible name.");
        foreach (var codec in cdCodecChoices.Concat(dvdCodecChoices))
            if (!Announces(codec))
                throw new InvalidOperationException($"{page}: a codec choice has no accessible name.");
        foreach (var help in helpButtons)
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(help)) ||
                ToolTipService.GetToolTip(help) == null)
                throw new InvalidOperationException($"{page}: an unnamed help button is reachable.");
        foreach (var command in new Button[] { start, stop, stopNow, openOutput })
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetHelpText(command)))
                throw new InvalidOperationException(
                    $"{page}: '{AutomationProperties.GetName(command)}' has no help text.");
    }

    /// <summary>
    /// A stat value that wraps inside its tile is a squeezed tile, however legible it
    /// still looks. WinUI exposes no line count, so a wrapped readout shows up as
    /// extra height; the disk readout is passed as multi-line because it is two lines
    /// by design.
    /// </summary>
    void AssertTileFits(FrameworkElement tile, TextBlock value, string where, bool singleLine = true)
    {
        if (tile.ActualWidth <= 0)
            throw new InvalidOperationException($"{where}: a stat tile has no width.");
        double line = value.FontSize * 1.5;
        if (singleLine && value.ActualHeight > line + 0.5)
            throw new InvalidOperationException(
                $"{where}: '{value.Text}' wraps inside a {tile.ActualWidth:0}px tile "
                + $"({value.ActualHeight / line:0.0} lines).");
    }

    // The Bento must agree with the width that actually drove it, not with a
    // hardcoded grid position, so this stays true at any DPI or window size. Inside
    // the hysteresis band the previous decision is kept on purpose, so only the
    // internal consistency is checked there.
    void AssertBento(string where)
    {
        double width = ((FrameworkElement)pages[0]).ActualWidth;
        bool applied = Grid.GetColumn(gameCard) == 1;
        if (lastStacked.HasValue)
        {
            if (lastStacked == applied)
                throw new InvalidOperationException(
                    $"{where}: game card at column {Grid.GetColumn(gameCard)} contradicts the latch.");
            if (Math.Abs(width - StackWidth) >= Hysteresis && lastStacked != (width < StackWidth))
                throw new InvalidOperationException(
                    $"{where}: page {width:0}px crosses {StackWidth:0} but the latch says {lastStacked}.");
        }
        if (Grid.GetRow(gameCard) != (applied ? 0 : 1))
            throw new InvalidOperationException(
                $"{where}: game card at row {Grid.GetRow(gameCard)}.");
    }

    // An equal row must restack exactly when its measured width crosses the
    // threshold, and its cells must sit where the applied state says they should.
    void AssertRow(Grid row, double threshold, bool? latched, FrameworkElement[] cells,
        string where)
    {
        bool applied = Grid.GetColumn(cells[1]) == 1;
        if (latched.HasValue)
        {
            if (latched == applied)
                throw new InvalidOperationException(
                    $"{where}: the cells contradict the latch.");
            if (Math.Abs(row.ActualWidth - threshold) >= Hysteresis &&
                latched != (row.ActualWidth < threshold))
                throw new InvalidOperationException(
                    $"{where}: {row.ActualWidth:0}px crosses {threshold:0} but the latch says {latched}.");
        }
        for (int i = 0; i < cells.Length; i++)
            if (Grid.GetRow(cells[i]) != (applied ? 0 : i))
                throw new InvalidOperationException(
                    $"{where}: cell {i} is at row {Grid.GetRow(cells[i])}.");
    }

    /// <summary>Checks every reflow against the width that actually drove it, so the
    /// assertions hold at any DPI, window size or pane state.</summary>
    void AssertReflow(string where)
    {
        AssertBento(where);
        AssertRow(metricRow, MetricStackWidth, lastStackedTiles, metricTiles, where + " tiles");
        AssertRow(legend, LegendStackWidth, lastStackedCaptions, legendItems, where + " captions");
        AssertRow(settingsPanels, SettingsStackWidth, lastStackedCards, settingsCards,
            where + " settings cards");
        AssertTileFits(metricTiles[0], elapsed, where);
        AssertTileFits(metricTiles[1], cpu, where);
        AssertTileFits(metricTiles[2], disk, where, singleLine: false);
        AssertCommandsFit(where);
        if (telemetry.ActualWidth <= 0 || telemetry.ActualHeight <= 0)
            throw new InvalidOperationException($"{where}: the telemetry strip collapsed.");
        if (navigation.Content == null)
            throw new InvalidOperationException($"{where}: navigation has no page content.");
    }

    // The commands must fit the column they live in, whichever way they are arranged.
    void AssertCommandsFit(string where)
    {
        foreach (var command in actions.Children.OfType<FrameworkElement>())
            if (command.ActualWidth <= 0)
                throw new InvalidOperationException($"{where}: a command button has no width.");
        // The row decides between one line and a stack from its own measured width;
        // checking that decision catches a one-way latch in either direction, which
        // the fit test alone cannot see once the row is stacked.
        bool needsStack = NaturalWidth(actions) + VisualTheme.GapM > actions.ActualWidth;
        if (needsStack != (actions.Orientation == Orientation.Vertical))
            throw new InvalidOperationException(
                $"{where}: the command row is {actions.Orientation} but needs "
                + $"{(needsStack ? "a stack" : "one line")} at {actions.ActualWidth:0}px "
                + $"({NaturalWidth(actions):0}px needed).");
    }

    async void RunSmoke(string destination)
    {
        string narrowTelemetry = "", narrowPage = "", minimalTelemetry = "", minimalPage = "",
            restoredPage = "";
        try
        {
            await Task.Delay(4000);
            Report("Entrada 1 / 1: UI diagnostics");
            // The saved language must reach the shared control labels, which are set
            // once at startup and are not part of any page build.
            if (resetDefaults.Content as string != Localization.T("Reset defaults") ||
                online.Content as string != Localization.T("Identify games by serial") ||
                advanced.Header as string != Localization.T("Encoding options") ||
                input.PlaceholderText != Localization.T("Folder containing your games"))
                throw new InvalidOperationException(
                    $"Startup labels ignored the saved language ({Localization.Language}).");
            await Task.Delay(100);
            var facts = new GameInfo
            {
                Title = "WinUI 3 test <safe>", Serial = "SLUS-21296", Type = "CD", System = "PS2",
                CurrentFormat = "CHD v5", CurrentSize = 1048576, SourceFormat = "RAR", SourceSize = 2097152,
                Source = "PRIVATE_SOURCE_PATH", Image = "PRIVATE_IMAGE_PATH",
                DatabaseUrl = "https://private.invalid/database", CoverUrl = "https://private.invalid/cover",
                Detection = "PRIVATE_DETECTION", Lookup = "PRIVATE_LOOKUP", Detail = "PRIVATE_DETAIL"
            };
            string factsHtml = GameHtml.Render(facts, null);
            if (factsHtml.Contains("PRIVATE_") || factsHtml.Contains("private.invalid")
                || !factsHtml.Contains("&lt;safe&gt;") || !factsHtml.Contains("CHD v5")
                || !factsHtml.Contains("1.00 MiB") && !factsHtml.Contains("1,00 MiB"))
                throw new InvalidOperationException("Game facts exposed diagnostics or lost escaped values.");
            await game.UpdateAsync(facts, true);
            Report("Comprimindo: UI diagnostics");
            Report("Etapa: 42%");
            await RefreshTelemetry();
            await Task.Delay(2000);
            if (stage.Value != 42 || !clock.IsRunning || clock.Elapsed.TotalSeconds < 1)
                throw new InvalidOperationException("UI progress or timer failed.");
            Report("Lote PS2: 100% (1/1)");
            await Task.Delay(200);
            if (clock.IsRunning || batch.Value != 100)
                throw new InvalidOperationException("Batch completion did not update the UI.");
            if (helpButtons.Count < 12) throw new InvalidOperationException("Help buttons are missing.");
            SetSettingsEnabled(false);
            if (toolSettingsPage.IsEnabled)
                throw new InvalidOperationException("Tool settings remained enabled during conversion.");
            SetSettingsEnabled(true);
            int previousLanguage = language.SelectedIndex;
            language.SelectedIndex = 1;
            await Task.Delay(150);
            if (Localization.Language != "pt-BR" ||
                (string)((NavigationViewItem)navigation.MenuItems[1]).Content != "Configurações")
                throw new InvalidOperationException("Portuguese UI did not load.");
            // A label added by the new Bento must translate, not fall back to English.
            if (stageLegend?.Text != "Progresso da etapa")
                throw new InvalidOperationException(
                    $"Portuguese telemetry legend missing: '{stageLegend?.Text}'.");
            language.SelectedIndex = previousLanguage;
            await Task.Delay(150);
            ShowPage(0);
            await Task.Delay(150);
            string previousInput = input.Text, previousOutput = output.Text;
            output.Text = "";
            input.Text = Path.Combine(Path.GetTempPath(), "DiscForgeSmoke");
            await Task.Delay(150);
            if (output.Text != Path.Combine(input.Text, "otimizados"))
                throw new InvalidOperationException("Default output did not follow the input folder.");
            input.Text = previousInput;
            output.Text = previousOutput;
            dvdCodecChoices[4].IsChecked = true;
            if (dvdCodecChoices[4].IsChecked == true)
                throw new InvalidOperationException("The four-codec limit was not enforced.");
            bool?[] previousCd = cdCodecChoices.ConvertAll(choice => choice.IsChecked).ToArray();
            bool? previousLegacy = legacyCompatibility.IsChecked;
            legacyCompatibility.IsChecked = false;
            foreach (var choice in cdCodecChoices) choice.IsChecked = false;
            if (CodecSelectionError() is not string codecMessage || !codecMessage.Contains("CD"))
                throw new InvalidOperationException("Missing CD codecs were not explained.");
            legacyCompatibility.IsChecked = true;
            if (cdHunk.IsEnabled || dvdHunk.IsEnabled || cdCodecChoices[0].IsEnabled ||
                dvdCodecChoices[0].IsEnabled || CodecSelectionError() != null ||
                !legacyWarning.IsOpen)
                throw new InvalidOperationException("Legacy profile did not override modern encoding options.");
            legacyCompatibility.IsChecked = previousLegacy;
            for (int i = 0; i < cdCodecChoices.Count; i++) cdCodecChoices[i].IsChecked = previousCd[i];
            await SaveSnapshotAsync(destination + ".png");
            AssertReflow("Default");
            AssertAccessibility("Conversion default");
            string normalTelemetry = $"{telemetry.ActualWidth:0}×{telemetry.ActualHeight:0}";
            string normalPage = $"{((FrameworkElement)pages[0]).ActualWidth:0}×{((FrameworkElement)pages[0]).ActualHeight:0}";
            advanced.IsExpanded = true;
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".advanced.png");
            advanced.IsExpanded = false;
            await game.SavePreviewAsync(destination + ".game.png");
            await telemetry.SavePreviewAsync(destination + ".telemetry.png");

            var presenter = (Microsoft.UI.Windowing.OverlappedPresenter)AppWindow.Presenter;
            presenter.Maximize();
            await Task.Delay(300);
            var root = (Microsoft.UI.Xaml.FrameworkElement)Content;
            await SaveSnapshotAsync(destination + ".maximized.png");
            // The shell must fill the client area: navigation spans the full
            // width and takes everything below the title bar row.
            if (Math.Abs(root.ActualWidth - navigation.ActualWidth) > 1 ||
                Math.Abs(root.ActualHeight - navigation.ActualHeight - TitleBarHeight) > 1)
                throw new InvalidOperationException($"Maximized layout: root {root.ActualWidth}×{root.ActualHeight}; navigation {navigation.ActualWidth}×{navigation.ActualHeight}.");
            if (Math.Abs(titleBar.ActualHeight - TitleBarHeight) > 1)
                throw new InvalidOperationException($"Title bar height {titleBar.ActualHeight}.");
            // Mica is the backdrop unless high contrast asks for the solid canvas.
            if (MicaController.IsSupported() && !VisualTheme.HighContrast && !micaActive)
                throw new InvalidOperationException("Mica is supported but the backdrop was not enabled.");
            AssertReflow("Wide");
            // Wide: the pane is expanded, which is what the wider page depends on.
            if (!navigation.IsPaneOpen)
                throw new InvalidOperationException("The navigation pane did not expand when wide.");
            string wideTelemetry = $"{telemetry.ActualWidth:0}×{telemetry.ActualHeight:0}";
            presenter.Restore();
            await Task.Delay(200);

            // Below the pane threshold the pane collapses to the icon rail, which
            // hands its width back to the page.
            AppWindow.Resize(new Windows.Graphics.SizeInt32(900, 900));
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".narrow.png");
            if (navigation.IsPaneOpen)
                throw new InvalidOperationException("The navigation pane did not collapse when narrow.");
            AssertReflow("Narrow");
            narrowTelemetry = $"{telemetry.ActualWidth:0}×{telemetry.ActualHeight:0}";
            narrowPage = $"{((FrameworkElement)pages[0]).ActualWidth:0}×{((FrameworkElement)pages[0]).ActualHeight:0}";
            AssertAccessibility("Conversion narrow");
            // Narrowest usable window: the page must still lay out in full.
            AppWindow.Resize(new Windows.Graphics.SizeInt32(620, 900));
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".minimal.png");
            AssertReflow("Minimal");
            minimalTelemetry = $"{telemetry.ActualWidth:0}×{telemetry.ActualHeight:0}";
            minimalPage = $"{((FrameworkElement)pages[0]).ActualWidth:0}×{((FrameworkElement)pages[0]).ActualHeight:0}";
            // A language change throws away every grid the reflow latches guard. The
            // rebuilt pages must reach the same arrangement at the SAME width, so
            // this is checked before the window is restored.
            int before = language.SelectedIndex;
            language.SelectedIndex = before == 0 ? 1 : 0;
            await Task.Delay(400);
            ShowPage(0);
            await Task.Delay(200);
            AssertReflow("Minimal after language");
            await SaveSnapshotAsync(destination + ".minimal-ptbr.png");
            language.SelectedIndex = before;
            await Task.Delay(400);
            ShowPage(0);
            await Task.Delay(200);
            AssertReflow("Minimal restored language");
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1380, 1000));
            await Task.Delay(200);
            AssertReflow("Restored");
            restoredPage = $"{((FrameworkElement)pages[0]).ActualWidth:0}×{((FrameworkElement)pages[0]).ActualHeight:0}";
            ShowPage(2);
            await Task.Delay(150);
            aboutChanges.IsExpanded = true;
            await Task.Delay(300);
            await SaveSnapshotAsync(destination + ".about.png");
            ShowPage(0);
            await File.WriteAllTextAsync(destination, "WinUI3=OK\nWebView2=" + game.BrowserStatus
                + "\nTelemetry=" + telemetry.Status
                + "\nNavigation=OK\nMica=" + (micaActive ? "OK" : "fallback")
                + "\nTitleBar=OK\nShell=OK\nMaximized layout=OK\nCHD v4 warning=OK"
                + "\nProgress/Timer=OK\nTool settings=OK\nThreads=" + threads.Value
                + "\nTelemetry normal=" + normalTelemetry + " wide=" + wideTelemetry
                + " narrow=" + narrowTelemetry + " minimal=" + minimalTelemetry
                + "\nPage normal=" + normalPage + " narrow=" + narrowPage
                + " minimal=" + minimalPage + " restored=" + restoredPage
                + "\nHighContrast=" + (VisualTheme.HighContrast ? "on" : "off")
                + "\nAnimations=" + (VisualTheme.AnimationsEnabled ? "on" : "off")
                + "\nStartup language=OK\nAccessibility=OK\nReflow after language=OK"
                + "\nCPU=" + cpu.Text + "\nDisk=" + disk.Text);
        }
        catch (Exception ex) { await File.WriteAllTextAsync(destination, ex.ToString()); }
        finally { Close(); }
    }
}
