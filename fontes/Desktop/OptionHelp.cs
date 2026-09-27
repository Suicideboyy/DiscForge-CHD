/// <summary>Explanations shared by tooltips and accessible help buttons.</summary>
static class OptionHelp
{
    public const string Input = "Folder containing your games. PS2 accepts images and archives; PS1 processes CHDs. Previously converted games are checked before extraction.";
    public const string Output = "Verified CHDs are saved here. The default is the otimizados folder within the selected input folder. Originals are preserved unless removal is enabled.";
    public const string Platform = "PS1 optimizes existing CHDs. PS2 accepts archives, ISO, BIN/CUE and CHD, using createcd for CDs and createdvd for DVDs.";
    public const string Threads = "Defaults to this machine's logical processor count. More threads may speed up chdman, depending on the CPU, codecs and storage.";
    public const string CdHunk = "CD block size in bytes, in multiples of 2448. Larger blocks may compress better but cost more to read a small section.";
    public const string DvdHunk = "DVD block size in bytes, in multiples of 2048. Larger blocks may improve compression but increase work per read.";
    public const string CdCodecs = "Select up to four CD codecs. cdlz uses LZMA, cdzs uses Zstandard, cdzl uses zlib and cdfl uses FLAC for audio. chdman chooses the smallest result per block.";
    public const string DvdCodecs = "Select up to four codecs. LZMA favors size, Zstandard balances speed and size, zlib is general purpose, FLAC targets audio and Huffman suits some repeated patterns.";
    public const string Lookup = "Looks up the game serial for its name and media type. A confirmed match uses Name [SERIAL].chd. Cover lookup runs independently.";
    public const string Delete = "Removes an archive only when all its discs were converted and verified in this run. Existing outputs and failed inputs retain originals.";
    public const string Stop = "Finishes the current input and verification, then stops before the next game.";
    public const string StopNow = "Interrupts the current task, clears its temporary files and preserves the original input. Outputs completed earlier remain available.";
    public const string ChdmanPath = "Path to a custom chdman.exe. Leave blank to use the bundled version.";
    public const string SevenZipExePath = "Path to a custom 7z.exe for unsupported archives. Leave blank to use the bundled version.";
    public const string SevenZipDllPath = "Path to the 7z.dll paired with the selected 7z.exe. Leave blank to use the bundled version.";
    public const string Cpu = "CPU used by DiscForge and the converters it launched, sampled about once per second. N/A means unavailable.";
    public const string Disk = "Bytes read and written by DiscForge and the converters it launched, shown in MiB/s. Windows caching can affect these values.";
    public const string Time = "Elapsed time for the current input, including identification, extraction, encoding and verification.";
}
