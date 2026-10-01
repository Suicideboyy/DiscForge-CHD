
namespace DiscForge;

public sealed partial class DesktopApp : Application
{
    readonly string[] arguments;
    MainWindow window;

    public DesktopApp(string[] arguments)
    {
        this.arguments = arguments;
        RequestedTheme = ApplicationTheme.Light;
        UnhandledException += (_, e) => CrashReporter.Record(e.Exception, "WinUI.UnhandledException");
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new MainWindow(arguments);
        window.Activate();
    }
}
