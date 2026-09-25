using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DiscForge;

public sealed partial class DesktopApp : Application
{
    readonly string[] arguments;
    MainWindow window;

    public DesktopApp(string[] arguments)
    {
        this.arguments = arguments;
        RequestedTheme = ApplicationTheme.Light;
        UnhandledException += (_, e) => System.IO.File.WriteAllText(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chd-winui-error.txt"), e.Exception.ToString());
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new MainWindow(arguments);
        window.Activate();
    }
}
