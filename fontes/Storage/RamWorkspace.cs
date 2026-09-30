using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

sealed class RamWorkspace : IDisposable
{
    const ulong OverheadBytes = 256UL * 1024 * 1024;
    const uint QueryImDiskDevice = (0x8372U << 16) | (0x802U << 2);
    readonly string volumeRoot;
    readonly string workspaceParent;
    public string Root { get; }

    RamWorkspace(string volumeRoot, string selectedFolder)
    {
        this.volumeRoot = volumeRoot;
        workspaceParent = Path.GetFullPath(selectedFolder);
        Root = Path.Combine(workspaceParent, "DiscForge-" + Guid.NewGuid().ToString("N"));
        FileSystemPaths.EnsureNoLinks(Root);
        Directory.CreateDirectory(Root);
    }

    public static bool Fits(ulong required, ulong availableMemory, ulong freeVolume)
    {
        if (required > ulong.MaxValue - OverheadBytes) return false;
        ulong allocation = required + OverheadBytes;
        return freeVolume >= allocation && availableMemory >= MemoryStatus.ReserveBytes
            && allocation <= availableMemory - MemoryStatus.ReserveBytes;
    }

    public static RamWorkspace TryCreate(string selectedPath, long archiveBytes, out string reason)
    {
        if (!TryValidateDrive(selectedPath, out string root, out reason)) return null;
        if (!CheckCapacity(root, archiveBytes, out reason)) return null;
        try { return new RamWorkspace(root, selectedPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            reason = "Unable to create the RAM workspace: " + ex.Message;
            return null;
        }
    }

    public bool CanFit(long additionalBytes, out string reason) =>
        CheckCapacity(volumeRoot, additionalBytes, out reason);

    static bool CheckCapacity(string root, long bytes, out string reason)
    {
        reason = "RAM extraction requires more than 12 GiB total and at least 5 GiB free.";
        if (!MemoryStatus.TryRead(out var memory) || !memory.Eligible) return false;
        if (bytes <= 0)
        {
            reason = "Archive size cannot be estimated safely.";
            return false;
        }
        try
        {
            if (Fits((ulong)bytes, memory.AvailableBytes, (ulong)new DriveInfo(root).AvailableFreeSpace))
            {
                reason = "";
                return true;
            }
            reason = "Insufficient RAM or RAM-drive space for this archive and the 3 GiB reserve.";
        }
        catch (IOException ex) { reason = "RAM drive unavailable: " + ex.Message; }
        return false;
    }

    // Query only existing volumes. A label or a drive letter does not prove RAM backing.
    public static bool TryValidateDrive(string selectedPath, out string root, out string reason)
    {
        root = "";
        reason = "Select an existing RAM drive (Windows RAM drive or ImDisk memory drive).";
        try
        {
            if (string.IsNullOrWhiteSpace(selectedPath)) return false;
            string full = Path.GetFullPath(selectedPath);
            root = Path.GetPathRoot(full);
            if (root == null || root.Length != 3 || root[1] != ':' || !Directory.Exists(full)) return false;
            FileSystemPaths.EnsureNoLinks(full);
            var drive = new DriveInfo(root);
            if (!drive.IsReady) return false;
            if (drive.DriveType != DriveType.Ram && !IsImDiskMemory(root)) return false;
            reason = "";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            reason = "RAM-drive verification failed: " + ex.Message;
            return false;
        }
    }

    static bool IsImDiskMemory(string root)
    {
        using var device = CreateFile(@"\\.\" + root[..2], 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (device.IsInvalid) return false;
        byte[] data = new byte[65536];
        if (!DeviceIoControl(device, QueryImDiskDevice, IntPtr.Zero, 0, data,
            (uint)data.Length, out uint returned, IntPtr.Zero) || returned < 48) return false;
        // IMDISK_CREATE_DATA: DWORD + padding + DISK_GEOMETRY + LARGE_INTEGER, then Flags.
        // Source: https://github.com/LTRData/ImDisk/blob/master/inc/imdisk.h
        uint flags = BitConverter.ToUInt32(data, 40);
        return (flags & 1) == 0 && ((flags & 0xF00) == 0x200
            || (flags & 0xF00) == 0x100 && (flags & 0xF000) == 0x1000);
    }

    // Delete only our unique folder; leave the user's mounted drive and other contents intact.
    public void Dispose() => FileSystemPaths.DeleteWorkDirectory(Root, workspaceParent);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security,
        uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DeviceIoControl(SafeFileHandle device, uint control, IntPtr input, uint inputSize,
        [Out] byte[] output, uint outputSize, out uint returned, IntPtr overlapped);
}
