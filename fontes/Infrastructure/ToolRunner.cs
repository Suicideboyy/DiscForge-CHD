
/// <summary>Runs isolated converters, streams bounded diagnostics, and owns cancellation.</summary>
sealed class ToolRunner(Action<string> report, Func<string> workingDirectory)
{
    public async Task<ProcessResult> RunAsync(string name, string[] args, bool progress)
    {
        var start = new ProcessStartInfo(Path.Combine(BundledTools.Tools, name))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = workingDirectory()
        };
        foreach (string argument in args) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        Engine.ThrowIfCancelled();
        process.Start();
        AppProcessRegistry.Register(process);
        using var cancellation = Engine.Token.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (Exception) { } // The converter may have exited concurrently.
        });
        try
        {
            process.StandardInput.Close();
            var text = new StringBuilder();
            object gate = new();
            bool truncated = false;
            void Emit(string value)
            {
                lock (gate)
                {
                    if (text.Length + value.Length < 16000000) text.AppendLine(value);
                    else truncated = true;
                }
                if (!progress) return;
                var match = Regex.Match(value, @"(?<!\d)(\d{1,3}(?:[.,]\d+)?)\s*%");
                if (match.Success) report("Etapa: " + match.Groups[1].Value + "%");
            }
            await Task.WhenAll(ReadLinesAsync(process.StandardOutput, Emit),
                ReadLinesAsync(process.StandardError, Emit)).ConfigureAwait(false);
            await process.WaitForExitAsync(Engine.Token).ConfigureAwait(false);
            Engine.ThrowIfCancelled();
            if (truncated)
                throw new IOException("Tool output exceeded the limit; "
                    + "operation stopped to prevent an incomplete listing.");
            if (progress && process.ExitCode == 0) report("Etapa: 100%");
            return new ProcessResult { Code = process.ExitCode, Text = text.ToString() };
        }
        finally { AppProcessRegistry.Unregister(process.Id); }
    }

    // Native tools redraw progress with carriage returns; ordinary ReadLine misses these updates.
    static async Task ReadLinesAsync(StreamReader reader, Action<string> emit)
    {
        char[] buffer = new char[1024];
        var line = new StringBuilder();
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) > 0)
        {
            foreach (char value in buffer.AsSpan(0, count))
            {
                if (value is '\r' or '\n' or '\b')
                {
                    if (line.Length == 0) continue;
                    emit(line.ToString());
                    line.Clear();
                }
                else if (line.Length < 1000000) line.Append(value);
            }
        }
        if (line.Length > 0) emit(line.ToString());
    }

    public async Task<string> RequireSuccessAsync(string name, bool progress, params string[] args)
    {
        var result = await RunAsync(name, args, progress).ConfigureAwait(false);
        if (result.Code != 0)
            throw new IOException($"{name} (exit code {result.Code}): "
                + result.Text[Math.Max(0, result.Text.Length - 3000)..]);
        return result.Text;
    }
}