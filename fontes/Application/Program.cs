using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            CrashReporter.Initialize();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (args.Length == 2 && args[0] == "--tools-test")
            {
                BundledTools.Stage(new EncoderSettings { ChdmanPath = args[1] });
                return File.Exists(Path.Combine(BundledTools.Tools, "chdman.exe")) ? 0 : 1;
            }
            if (args.Length == 3 && args[0] == "--archive-test")
            {
                BundledTools.Stage();
                Directory.CreateDirectory(args[2]);
                var messages = new List<string>();
                var reader = new ArchiveReader(messages.Add);
                reader.ListAsync(args[1], args[2]).GetAwaiter().GetResult();
                reader.ExtractAsync(args[1], args[2]).GetAwaiter().GetResult();
                File.WriteAllLines(args[2] + ".log", messages);
                return 0;
            }
            if (args.Length == 3 && args[0] == "--report-test")
            {
                File.Copy(IssueReport.Create(args[1]), args[2], true);
                return new FileInfo(args[2]).Length <= IssueReport.MaxBytes ? 0 : 1;
            }
            if (args.Length == 4 && (args[0] is "--run-test" or "--stop-test" or "--delete-test" or "--cancel-test" or "--auto-test" or "--ram-test" or "--legacy-test"))
                return RunTest(args);
            if (args.Length == 3 && (args[0] is "--cover-test" or "--cover-test-ps1"))
            {
                byte[] image = CoverService.Load(args[1], args[0] != "--cover-test-ps1").GetAwaiter().GetResult();
                if (image == null)
                    return 3;
                File.WriteAllBytes(args[2], image);
                return 0;
            }
            using var mutex = new Mutex(true, @"Local\CHDOptimizerDesktop", out bool created);
            if (!created)
                return 1;
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start(parameters =>
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
                _ = new DiscForge.DesktopApp(args);
            });
            return 0;
        }
        catch (Exception ex)
        {
            CrashReporter.Record(ex, "Program.Main");
            return 1;
        }
    }

    static int RunTest(string[] args)
    {
        var settings = new EncoderSettings
        {
            Input = args[1],
            Output = args[2],
            Platform = args[3],
            AutoDetectSystem = args[0] == "--auto-test",
            UseRamExtraction = args[0] == "--ram-test",
            RamDiskPath = args[0] == "--ram-test" ? args[1] : "",
            Online = false,
            LegacyCompatibility = args[0] == "--legacy-test",
            Delete = args[0] == "--delete-test",
            Threads = Math.Min(4, Environment.ProcessorCount)
        };
        var lines = new List<string>();
        int code = Engine.Run(settings, line =>
        {
            lock (lines)
                lines.Add(line);
            if (args[0] == "--cancel-test" && (line.StartsWith("Stage:") || line.StartsWith("Etapa:")))
                Engine.CancelNow();
            if (args[0] == "--stop-test" && (line.StartsWith("Type:") || line.StartsWith("Tipo:")) && Engine.StopFile != null)
                File.WriteAllText(Engine.StopFile, "");
        }).GetAwaiter().GetResult();
        File.WriteAllLines(Path.Combine(settings.Input, "app-test.log"), lines, Encoding.UTF8);
        return code;
    }
}
