using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

sealed partial class MainWindow
{
    /// <summary>Concentra o ciclo de vida da janela e impede fechar no meio de uma entrada.</summary>
    void ConnectEvents()
    {
        start.Click += async (_, _) => await StartAsync();
        stop.Click += (_, _) => RequestStop();
        platform.SelectionChanged += (_, _) => { UpdatePlatform(); game.Reset(); };
        AppWindow.Closing += (_, args) =>
        {
            if (!running) return;
            args.Cancel = true;
            RequestStop();
            status.Text = "Aguarde a entrada terminar antes de fechar.";
        };
        Closed += (_, _) =>
        {
            closed = true;
            timer.Stop();
            performance.Dispose();
            game.Dispose();
        };
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += async (_, _) => await RefreshTelemetry();
        timer.Start();
        UpdatePlatform();
    }

    UIElement BuildProgress()
    {
        var body = VisualTheme.Section("03", "Processamento");
        stage.Foreground = VisualTheme.Accent;
        batch.Foreground = VisualTheme.Teal;
        body.Children.Add(status);
        body.Children.Add(stage);
        body.Children.Add(batchStatus);
        body.Children.Add(batch);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        start.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        start.Background = VisualTheme.Accent;
        ToolTipService.SetToolTip(start, HelpText("Valida as opções e inicia a fila. Cada CHD é verificado antes de ser salvo."));
        ToolTipService.SetToolTip(stop, HelpText(OptionHelp.Stop));
        var open = new Button { Content = "Abrir saída", MinHeight = 42 };
        ToolTipService.SetToolTip(open, "Abre a pasta de destino no Explorador de Arquivos.");
        open.Click += (_, _) =>
        {
            try
            {
                if (Directory.Exists(output.Text))
                    Process.Start(new ProcessStartInfo(output.Text) { UseShellExecute = true });
            }
            catch (Exception ex) { Append("Abrir saída: " + ex.Message); }
        };
        actions.Children.Add(start);
        actions.Children.Add(stop);
        actions.Children.Add(open);
        body.Children.Add(actions);
        body.Children.Add(new Expander
        {
            Header = "Registro da execução", Content = log,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        });
        return VisualTheme.Card(body);
    }
}
