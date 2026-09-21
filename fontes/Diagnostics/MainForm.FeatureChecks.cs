using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

partial class MainForm
{
    internal static int VerifyFeatures(string reportPath)
    {
        var results = new List<string>();
        using (var form = new MainForm())
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-10000, -10000);
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();
            Require(form.threads.Value == MachineInfo.LogicalProcessors, "Threads da interface");
            Require(new EncoderSettings().Threads == MachineInfo.LogicalProcessors, "Threads padrão");
            results.Add("Threads detectadas: " + MachineInfo.LogicalProcessors);

            int requests = 0;
            form.coverLoader = serial =>
            {
                ++requests;
                return Task.FromResult<Image>(requests == 1 ? null : new Bitmap(32, 48));
            };
            form.UpdateGame(new Dictionary<string, object>
            {
                { "Serial", "SLUS-21693" }, { "Type", "DVD" }, { "Status", "JÁ EXISTENTE" }
            });
            Require(form.cover.Image == null && requests == 1, "Primeira leitura falhou como esperado");
            form.UpdateGame(new Dictionary<string, object>
            {
                { "Serial", "SLUS-21693" }, { "Type", "DVD" }, { "Status", "JÁ EXISTENTE" }
            });
            Require(form.cover.Image != null && requests == 2, "Nova leitura com mídia já identificada");
            results.Add("PASSOU: capa repetida após falha, com DVD já existente.");

            var pending = new TaskCompletionSource<Image>();
            form.coverLoader = serial => serial == "SLUS-20091"
                ? pending.Task : Task.FromResult<Image>(new Bitmap(20, 20));
            Task first = form.RefreshCover("SLUS-20091");
            Task duplicate = form.RefreshCover("SLUS-20091");
            Require(duplicate.IsCompleted && !first.IsCompleted, "Requisições duplicadas agrupadas");
            form.RefreshCover("SLUS-21296").GetAwaiter().GetResult();
            pending.SetResult(new Bitmap(10, 10));
            PumpUntil(() => first.IsCompleted, 5000);
            Require(form.cover.Image.Width == 20, "Resposta antiga não substitui a capa atual");
            results.Add("PASSOU: troca rápida de jogos e requisições simultâneas.");

            form.BeginEntry();
            PumpUntil(() => form.taskClock.ElapsedMilliseconds >= 1100, 5000);
            form.UpdateElapsed();
            Require(form.elapsedValue.Text != "00:00:00", "Cronômetro da entrada");
            form.taskClock.Stop();
            long stopped = form.taskClock.ElapsedTicks;
            Thread.Sleep(100);
            Require(form.taskClock.ElapsedTicks == stopped, "Cronômetro parado");
            results.Add("PASSOU: início, atualização e parada do cronômetro.");

            Require(AppChangelog.Text.Contains("1.2.0"), "Início do histórico C#");
            Require(!AppChangelog.Text.Contains("1.1.0") && !AppChangelog.Text.Contains("1.2.1"),
                "Histórico sem versões antigas ou alterações internas");
            results.Add("PASSOU: changelog somente de funções e correções desde 1.2.0.");
        }

        using (var monitor = new SystemPerformance())
        {
            monitor.Read();
            Thread.Sleep(1100);
            var sample = monitor.Read();
            Require(!sample.CpuPercent.HasValue || sample.CpuPercent >= 0 && sample.CpuPercent <= 100,
                "Percentual de CPU válido");
            Require(!sample.ReadBytesPerSecond.HasValue || sample.ReadBytesPerSecond >= 0,
                "Taxa de leitura válida");
            results.Add("CPU: " + (sample.CpuPercent.HasValue ? sample.CpuPercent.Value.ToString("F1") : "N/D"));
            results.Add("Leitura: " + Rate(sample.ReadBytesPerSecond));
            results.Add("Escrita: " + Rate(sample.WriteBytesPerSecond));
        }
        File.WriteAllLines(reportPath, results);
        return 0;
    }

    static void Require(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Teste falhou: " + name);
        }
    }

    static void PumpUntil(Func<bool> completed, int timeout)
    {
        var watch = Stopwatch.StartNew();
        while (!completed() && watch.ElapsedMilliseconds < timeout)
        {
            Application.DoEvents();
            Thread.Sleep(15);
        }
        Require(completed(), "Prazo de operação assíncrona");
    }
}
