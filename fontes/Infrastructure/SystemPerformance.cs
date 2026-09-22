using System;
using System.Runtime.InteropServices;

sealed class PerformanceSample
{
    public double? CpuPercent;
    public double? ReadBytesPerSecond;
    public double? WriteBytesPerSecond;
    public double? DiskBusyPercent;
}

// Contadores totais do Windows; incluem outros aplicativos e todos os discos.
sealed class SystemPerformance : IDisposable
{
    readonly object gate = new object();
    IntPtr query;
    IntPtr cpu;
    IntPtr read;
    IntPtr write;
    IntPtr idle;
    bool initialized;
    bool disposed;

    public PerformanceSample Read()
    {
        lock (gate)
        {
            var sample = new PerformanceSample();
            if (disposed)
            {
                return sample;
            }

            if (!initialized)
            {
                Initialize();
            }

            if (query == IntPtr.Zero || PdhCollectQueryData(query) != 0)
            {
                return sample;
            }

            sample.CpuPercent = Percent(Value(cpu));
            sample.ReadBytesPerSecond = Value(read);
            sample.WriteBytesPerSecond = Value(write);
            double? idlePercent = Value(idle);
            sample.DiskBusyPercent = idlePercent.HasValue ? Percent(100 - idlePercent.Value) : null;
            return sample;
        }
    }

    void Initialize()
    {
        initialized = true;
        if (PdhOpenQuery(null, UIntPtr.Zero, out query) != 0)
        {
            query = IntPtr.Zero;
            return;
        }

        cpu = Add(@"\Processor Information(_Total)\% Processor Time");
        if (cpu == IntPtr.Zero)
        {
            cpu = Add(@"\Processor(_Total)\% Processor Time");
        }
        read = Add(@"\PhysicalDisk(_Total)\Disk Read Bytes/sec");
        write = Add(@"\PhysicalDisk(_Total)\Disk Write Bytes/sec");
        idle = Add(@"\PhysicalDisk(_Total)\% Idle Time");
    }

    IntPtr Add(string path)
    {
        IntPtr counter;
        return PdhAddEnglishCounter(query, path, UIntPtr.Zero, out counter) == 0
            ? counter : IntPtr.Zero;
    }

    static double? Value(IntPtr counter)
    {
        if (counter == IntPtr.Zero)
        {
            return null;
        }

        CounterValue value;
        uint type;
        uint status = PdhGetFormattedCounterValue(counter, 0x00000200, out type, out value);
        if (status != 0 || value.Status > 0 || Double.IsNaN(value.Number)
            || Double.IsInfinity(value.Number))
        {
            return null;
        }

        return Math.Max(0, value.Number);
    }

    static double? Percent(double? value)
    {
        return value.HasValue ? (double?)Math.Max(0, Math.Min(100, value.Value)) : null;
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            if (query != IntPtr.Zero)
            {
                PdhCloseQuery(query);
                query = IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CounterValue
    {
        public uint Status;
        public double Number;
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, EntryPoint = "PdhOpenQueryW")]
    static extern uint PdhOpenQuery(string source, UIntPtr userData, out IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, EntryPoint = "PdhAddEnglishCounterW")]
    static extern uint PdhAddEnglishCounter(IntPtr query, string path, UIntPtr data, out IntPtr counter);

    [DllImport("pdh.dll")]
    static extern uint PdhCollectQueryData(IntPtr query);

    [DllImport("pdh.dll")]
    static extern uint PdhGetFormattedCounterValue(
        IntPtr counter, uint format, out uint type, out CounterValue value);

    [DllImport("pdh.dll")]
    static extern uint PdhCloseQuery(IntPtr query);
}
