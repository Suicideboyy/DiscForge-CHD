
static class Engine
{
    public static string StopFile;
    static CancellationTokenSource cancellation;
    public static CancellationToken Token => cancellation?.Token ?? CancellationToken.None;

    // Stops active extraction/encoding and lets the session clean its own temporary files.
    public static void CancelNow() => cancellation?.Cancel();

    public static void ThrowIfCancelled() => Token.ThrowIfCancellationRequested();
    // Prevents concurrent runs on one input folder and releases the stop request on exit.
    public static async Task<int> Run(EncoderSettings settings, Action<string> report)
    {
        settings.Validate();
        BundledTools.Stage(settings);
        Directory.CreateDirectory(settings.Output);
        string temp = Path.Combine(settings.Input, "temp");
        Directory.CreateDirectory(temp);
        FileSystemPaths.EnsureNoLinks(temp);
        string lockFile = Path.Combine(temp, "optimizer-app.lock");
        FileSystemPaths.EnsureNoLinks(lockFile);
        using (var guard = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.None))
        {
            StopFile = Path.Combine(BundledTools.Tools, "stop-" + Guid.NewGuid().ToString("N"));
            cancellation = new CancellationTokenSource();
            try
            {
                return await new ConversionSession(settings, report, temp).Run().ConfigureAwait(false);
            }
            finally
            {
                cancellation.Dispose();
                cancellation = null;
                if (File.Exists(StopFile))
                {
                    File.Delete(StopFile);
                }

                StopFile = null;
            }
        }
    }
}
