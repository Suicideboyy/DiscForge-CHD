using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

sealed class PerformanceSample
{
    public double? CpuPercent;
    public double? ReadBytesPerSecond;
    public double? WriteBytesPerSecond;
}

// Keeps converter totals after a short-lived child exits between UI samples.
static class AppProcessRegistry
{
    static readonly object Gate = new();
    static readonly Dictionary<int, Process> Children = new();
    static TimeSpan completedCpu;
    static ulong completedRead;
    static ulong completedWrite;

    public static void Register(Process process)
    {
        lock (Gate) Children[process.Id] = process;
    }

    public static void Unregister(int processId)
    {
        lock (Gate)
        {
            if (!Children.Remove(processId, out var process)) return;
            Accumulate(process, ref completedCpu, ref completedRead, ref completedWrite);
        }
    }

    public static (TimeSpan Cpu, ulong Read, ulong Write) Totals()
    {
        lock (Gate)
        {
            TimeSpan cpu = completedCpu;
            ulong read = completedRead, write = completedWrite;
            foreach (Process child in Children.Values)
                Accumulate(child, ref cpu, ref read, ref write);
            return (cpu, read, write);
        }
    }

    static void Accumulate(Process process, ref TimeSpan cpu, ref ulong read, ref ulong write)
    {
        try
        {
            process.Refresh();
            cpu += process.TotalProcessorTime;
            if (GetProcessIoCounters(process.Handle, out var counters))
            {
                read += counters.ReadTransferCount;
                write += counters.WriteTransferCount;
            }
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IoCounters
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
        public ulong ReadTransferCount, WriteTransferCount, OtherTransferCount;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetProcessIoCounters(IntPtr process, out IoCounters counters);

    public static (TimeSpan Cpu, ulong Read, ulong Write) OwnTotals()
    {
        using Process own = Process.GetCurrentProcess();
        TimeSpan cpu = TimeSpan.Zero;
        ulong read = 0, write = 0;
        Accumulate(own, ref cpu, ref read, ref write);
        return (cpu, read, write);
    }
}

// CPU and I/O describe DiscForge and its registered converter processes only.
sealed class SystemPerformance : IDisposable
{
    readonly object gate = new();
    DateTime lastTime;
    TimeSpan lastCpu;
    ulong lastRead, lastWrite;
    bool disposed;

    public PerformanceSample Read()
    {
        lock (gate)
        {
            var sample = new PerformanceSample();
            if (disposed) return sample;

            DateTime now = DateTime.UtcNow;
            var own = AppProcessRegistry.OwnTotals();
            var children = AppProcessRegistry.Totals();
            TimeSpan cpu = own.Cpu + children.Cpu;
            ulong read = own.Read + children.Read;
            ulong write = own.Write + children.Write;

            double seconds = (now - lastTime).TotalSeconds;
            if (lastTime != default && seconds > 0)
            {
                double cpuSeconds = (cpu - lastCpu).TotalSeconds;
                sample.CpuPercent = Math.Clamp(cpuSeconds / seconds / Environment.ProcessorCount * 100, 0, 100);
                sample.ReadBytesPerSecond = read >= lastRead ? (read - lastRead) / seconds : null;
                sample.WriteBytesPerSecond = write >= lastWrite ? (write - lastWrite) / seconds : null;
            }

            lastTime = now;
            lastCpu = cpu;
            lastRead = read;
            lastWrite = write;
            return sample;
        }
    }

    public void Dispose() { lock (gate) disposed = true; }
}
