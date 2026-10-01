
class EncoderSettings
{
    public string Input;
    public string Output;
    public string Platform = "PS2";
    public bool AutoDetectSystem = false;
    public bool LegacyCompatibility = false;
    public string Cd = "cdlz,cdzs,cdzl,cdfl";
    public string Dvd = "lzma,zstd,zlib,flac";
    public int CdHunk = 2448;
    public int DvdHunk = 2048;
    public int Threads = MachineInfo.LogicalProcessors;
    public bool Delete = false;
    public bool Online = true;
    public string ChdmanPath = "";
    public void Validate()
    {
        Input = Path.GetFullPath(Input).TrimEnd('\\');
        Output = Path.GetFullPath(Output).TrimEnd('\\');
        if (!Directory.Exists(Input))
            throw new Exception("Select an existing input folder.");

        if (Input.Length < 3 || Output.Length < 3)
            throw new Exception("Select a folder, not a drive root.");

        if (Input.Equals(Output, StringComparison.OrdinalIgnoreCase) || Input.StartsWith(Output + "\\",
            StringComparison.OrdinalIgnoreCase))
            throw new Exception("Output must differ from input and must not contain the input folder.");

        string temp = Input + "\\temp";
        if (Output.Equals(temp, StringComparison.OrdinalIgnoreCase) || Output.StartsWith(temp + "\\",
            StringComparison.OrdinalIgnoreCase))
            throw new Exception("Output cannot be inside the input temporary folder.");

        FileSystemPaths.EnsureNoLinks(Input);
        FileSystemPaths.EnsureNoLinks(Output);

        if (Platform != "PS1" && Platform != "PS2")
            throw new Exception("Invalid platform.");

        if (!LegacyCompatibility) CheckCodecs(Cd, new[]{"cdlz", "cdzs", "cdzl", "cdfl"}, "CD");
        if (!LegacyCompatibility && (Platform == "PS2" || AutoDetectSystem))
            CheckCodecs(Dvd, new[]{"lzma", "zstd", "zlib", "flac", "huff"}, "DVD");
        if (!LegacyCompatibility && (CdHunk < 2448 || CdHunk > 1048576 || CdHunk % 2448 != 0 || DvdHunk < 2048 || DvdHunk > 1048576
            || DvdHunk % 2048 != 0))
            throw new Exception("Invalid hunk size for the media.");

        if (Threads < 1 || Threads > MachineInfo.LogicalProcessors)
            throw new Exception("Invalid thread count.");
        BundledTools.ValidateSelectedTools(this);
    }

    static void CheckCodecs(string codecs, string[] allowed, string media)
    {
        if (string.IsNullOrWhiteSpace(codecs))
            throw new Exception($"Select at least one {media} codec in Encoding options.");
        string[] selected = codecs.Split(',');
        if (selected.Any(codec => !allowed.Contains(codec))
            || selected.Distinct().Count() != selected.Length)
            throw new Exception("Select valid codecs without duplicates.");
        if (selected.Length > 4)
            throw new Exception("Select up to four codecs per media type.");
    }
}
