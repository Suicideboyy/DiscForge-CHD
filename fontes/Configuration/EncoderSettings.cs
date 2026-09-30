using System;
using System.IO;
using System.Collections.Generic;

class EncoderSettings
{
    public string Input;
    public string Output;
    public string Platform = "PS2";
    public bool AutoDetectSystem = false;
    public string Cd = "cdlz,cdzs,cdzl,cdfl";
    public string Dvd = "lzma,zstd,zlib,flac";
    public int CdHunk = 2448;
    public int DvdHunk = 2048;
    public int Threads = MachineInfo.LogicalProcessors;
    public bool Delete = false;
    public bool Online = true;
    public string ChdmanPath = "";
    public bool UseRamExtraction;
    public string RamDiskPath = "";
    public void Validate()
    {
        Input = Path.GetFullPath(Input).TrimEnd('\\');
        Output = Path.GetFullPath(Output).TrimEnd('\\');
        if (!Directory.Exists(Input))
        {
            throw new Exception("Select an existing input folder.");
        }

        if (Input.Length < 3 || Output.Length < 3)
        {
            throw new Exception("Select a folder, not a drive root.");
        }

        if (Input.Equals(Output, StringComparison.OrdinalIgnoreCase) || Input.StartsWith(Output + "\\",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Output must differ from input and must not contain the input folder.");
        }

        string temp = Input + "\\temp";
        if (Output.Equals(temp, StringComparison.OrdinalIgnoreCase) || Output.StartsWith(temp + "\\",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Output cannot be inside the input temporary folder.");
        }

        foreach (string path in new[]{Input, Output})
        {
            for (DirectoryInfo d = new DirectoryInfo(path); d != null; d = d.Parent)
            {
                if (d.Exists && (d.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new Exception("Select folders without links or junctions.");
                }
            }
        }

        if (Platform != "PS1" && Platform != "PS2")
        {
            throw new Exception("Invalid platform.");
        }

        CheckCodecs(Cd, new[]{"cdlz", "cdzs", "cdzl", "cdfl"});
        CheckCodecs(Dvd, new[]{"lzma", "zstd", "zlib", "flac", "huff"});
        if (CdHunk < 2448 || CdHunk > 1048576 || CdHunk % 2448 != 0 || DvdHunk < 2048 || DvdHunk > 1048576
            || DvdHunk % 2048 != 0)
        {
            throw new Exception("Invalid hunk size for the media.");
        }

        if (Threads < 1 || Threads > MachineInfo.LogicalProcessors)
        {
            throw new Exception("Invalid thread count.");
        }
        BundledTools.ValidateSelectedTools(this);
    }

    static void CheckCodecs(string codecs, string[] allowed)
    {
        var set = new HashSet<string>();
        foreach (string c in codecs.Split(','))
        {
            if (Array.IndexOf(allowed, c) < 0 || !set.Add(c))
            {
                throw new Exception("Select valid codecs without duplicates.");
            }
        }

        if (set.Count > 4)
        {
            throw new Exception("Select up to four codecs per media type.");
        }
    }
}
