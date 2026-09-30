using System;
using System.Runtime.InteropServices;

readonly record struct MemorySnapshot(ulong TotalBytes, ulong AvailableBytes)
{
    public bool Eligible => MemoryStatus.IsEligible(TotalBytes, AvailableBytes);
}

static class MemoryStatus
{
    public const ulong GiB = 1024UL * 1024 * 1024;
    public const ulong ReserveBytes = 3 * GiB;

    public static bool IsEligible(ulong total, ulong available) => total > 12 * GiB
        && available >= 5 * GiB;

    // Recheck physical memory for every archive; a saved checkbox is not permission to overcommit.
    public static bool TryRead(out MemorySnapshot snapshot)
    {
        snapshot = default;
        var status = new NativeStatus { Length = (uint)Marshal.SizeOf<NativeStatus>() };
        if (!GlobalMemoryStatusEx(ref status)) return false;
        snapshot = new MemorySnapshot(status.TotalPhysical, status.AvailablePhysical);
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NativeStatus
    {
        public uint Length, Load;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile;
        public ulong TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx(ref NativeStatus status);
}
