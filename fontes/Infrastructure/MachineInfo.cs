using System;
using System.Runtime.InteropServices;

static class MachineInfo
{
    public static readonly int LogicalProcessors = DetectProcessors();

    static int DetectProcessors()
    {
        try
        {
            uint count = GetActiveProcessorCount(0xffff);
            if (count > 0 && count <= Int32.MaxValue)
            {
                return (int)count;
            }
        }
        catch (EntryPointNotFoundException)
        {
            // Windows sem a API de grupos de processadores.
        }

        return Math.Max(1, Environment.ProcessorCount);
    }

    [DllImport("kernel32.dll")]
    static extern uint GetActiveProcessorCount(ushort groupNumber);
}
