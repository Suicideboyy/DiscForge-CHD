using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

// Builds a small diagnostic bundle without copying disc images or archives.
static class IssueReport
{
    public const long MaxBytes = 10_000_000;
    const int MaxLogBytes = 1_000_000;

    public static string Create(string inputFolder)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiscForge CHD", "reports");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "DiscForge-report-"
            + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-"
            + Guid.NewGuid().ToString("N")[..8] + ".zip");

        var sources = new List<string>();
        if (Directory.Exists(CrashReporter.LogDirectory))
            sources.AddRange(Directory.GetFiles(CrashReporter.LogDirectory, "session-*.log")
                .OrderByDescending(File.GetLastWriteTimeUtc).Take(5));
        if (!string.IsNullOrWhiteSpace(inputFolder))
        {
            string conversionLog = Path.Combine(inputFolder, "temp", "log.txt");
            if (File.Exists(conversionLog)) sources.Add(conversionLog);
        }

        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var manifest = archive.CreateEntry("report.txt");
            using (var writer = new StreamWriter(manifest.Open(), Encoding.UTF8))
            {
                writer.WriteLine(AppInfo.DisplayName);
                writer.WriteLine("Created (UTC): " + DateTime.UtcNow.ToString("O"));
                writer.WriteLine("OS: " + Environment.OSVersion);
                writer.WriteLine("Runtime: " + Environment.Version);
                writer.WriteLine("Processor count: " + Environment.ProcessorCount);
                writer.WriteLine("Logs are included for review; game files are excluded.");
            }

            int index = 0;
            foreach (string source in sources)
            {
                byte[] tail = ReadTail(source, MaxLogBytes);
                var entry = archive.CreateEntry($"logs/{++index:00}-{Path.GetFileName(source)}",
                    CompressionLevel.Optimal);
                using var output = entry.Open();
                output.Write(tail);
            }
        }

        if (new FileInfo(path).Length <= MaxBytes) return path;
        File.Delete(path);
        throw new InvalidOperationException("The diagnostic report exceeded 10 MB.");
    }

    static byte[] ReadTail(string source, int maxBytes)
    {
        using var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        int size = (int)Math.Min(stream.Length, maxBytes);
        stream.Seek(-size, SeekOrigin.End);
        byte[] bytes = new byte[size];
        stream.ReadExactly(bytes);
        return bytes;
    }
}
