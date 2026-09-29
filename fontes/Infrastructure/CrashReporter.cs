using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Keeps bounded application diagnostics outside the game library.
static class CrashReporter
{
    static readonly object Gate = new();
    static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DiscForge CHD", "logs");
    static readonly string Session = Path.Combine(Root,
        "session-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-"
        + Guid.NewGuid().ToString("N")[..8] + ".log");
    static int initialized;

    public static string LogDirectory => Root;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref initialized, 1) != 0) return;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Record(args.ExceptionObject as Exception ?? new Exception("Unknown fatal error"),
                "AppDomain.UnhandledException");
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Record(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
        };
        Write("START " + AppInfo.DisplayName + " | Windows " + Environment.OSVersion.Version);
    }

    public static void Record(Exception exception, string context) =>
        Write("CRASH " + context + Environment.NewLine + exception);

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Root);
                string line = DateTime.UtcNow.ToString("O") + " " + message;
                if (line.Length > 32768) line = line[..32768] + " [truncated]";
                if (File.Exists(Session) && new FileInfo(Session).Length > 2_000_000)
                    return;
                File.AppendAllText(Session, line + Environment.NewLine, Encoding.UTF8);
                foreach (string old in Directory.GetFiles(Root, "session-*.log")
                    .OrderByDescending(File.GetLastWriteTimeUtc).Skip(5))
                    File.Delete(old);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
