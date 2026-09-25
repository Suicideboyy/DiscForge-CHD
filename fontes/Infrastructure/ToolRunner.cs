using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

sealed class ToolRunner
{
    readonly Action<string> report;
    readonly Func<string> workingDirectory;
    public ToolRunner(Action<string> report, Func<string> workingDirectory)
    {
        this.report = report;
        this.workingDirectory = workingDirectory;
    }

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
        foreach (string argument in args)
        {
            start.ArgumentList.Add(argument);
        }
        using (var process = new Process
        {
            StartInfo = start
        })
        {
            process.Start();
            process.StandardInput.Close();
            var text = new StringBuilder();
            object gate = new object();
            bool truncated = false;
            Action<string> line = value =>
            {
                lock (gate)
                {
                    if (text.Length + value.Length < 16000000)
                    {
                        text.AppendLine(value);
                    }
                    else
                    {
                        truncated = true;
                    }
                }

                if (progress)
                {
                    var m = Regex.Match(value, @"(?<!\d)(\d{1,3}(?:[.,]\d+)?)\s*%");
                    if (m.Success)
                    {
                        report("Etapa: " + m.Groups[1].Value + "%");
                    }
                }
            }

            ;
            await Task.WhenAll(ReadLinesAsync(process.StandardOutput, line),
                ReadLinesAsync(process.StandardError, line)).ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);
            if (truncated)
            {
                throw new IOException(
                    "Saída da ferramenta excedeu o limite; " +
                    "operação cancelada para evitar listagem incompleta.");
            }

            if (progress && process.ExitCode == 0)
            {
                report("Etapa: 100%");
            }

            return new ProcessResult
            {
                Code = process.ExitCode,
                Text = text.ToString()
            };
        }
    }

    static async Task ReadLinesAsync(StreamReader reader, Action<string> emit)
    {
        char[] chars = new char[1024];
        var line = new StringBuilder();
        int n;
        while ((n = await reader.ReadAsync(chars, 0, chars.Length).ConfigureAwait(false)) > 0)
        {
            for (int i = 0; i < n; i++)
            {
                char c = chars[i];
                if (c == '\r' || c == '\n' || c == '\b')
                {
                    if (line.Length > 0)
                    {
                        emit(line.ToString());
                        line.Clear();
                    }
                }
                else if (line.Length < 1000000)
                {
                    line.Append(c);
                }
            }
        }

        if (line.Length > 0)
        {
            emit(line.ToString());
        }
    }

    public async Task<string> RequireSuccessAsync(string name, bool progress, params string[] args)
    {
        var r = await RunAsync(name, args, progress).ConfigureAwait(false);
        if (r.Code != 0)
        {
            throw new IOException(name + " (código " + r.Code + "): " + r.Text.Substring(Math.Max(0,
                r.Text.Length - 3000)));
        }

        return r.Text;
    }
}
