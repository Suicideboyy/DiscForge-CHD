using System;
using System.IO;

sealed class FileSnapshot
{
    public string Path;
    public long Size;
    public long Ticks;
    public FileSnapshot(string path)
    {
        Path = path;
        var f = new FileInfo(path);
        Size = f.Length;
        Ticks = f.LastWriteTimeUtc.Ticks;
    }

    public bool Unchanged()
    {
        FileSystemPaths.EnsureNoLinks(Path);
        var f = new FileInfo(Path);
        return f.Exists && f.Length == Size && f.LastWriteTimeUtc.Ticks == Ticks;
    }
}
