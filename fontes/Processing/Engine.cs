using System;
using System.IO;
using System.Threading.Tasks;

static class Engine
{
    public static string StopFile;
    // Impede lotes simultâneos na mesma entrada e libera a parada ao encerrar.
    public static async Task<int> Run(EncoderSettings settings, Action<string> report)
    {
        settings.Validate();
        BundledTools.Stage();
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
            try
            {
                return await new ConversionSession(settings, report, temp).Run().ConfigureAwait(false);
            }
            finally
            {
                if (File.Exists(StopFile))
                {
                    File.Delete(StopFile);
                }

                StopFile = null;
            }
        }
    }
}
