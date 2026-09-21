using System;
using System.IO;
using System.Collections.Generic;

class EncoderSettings
{
    public string Input;
    public string Output;
    public string Platform = "PS2";
    public string Cd = "cdlz,cdzs,cdzl,cdfl";
    public string Dvd = "lzma,zstd,zlib,flac";
    public int CdHunk = 2448;
    public int DvdHunk = 2048;
    public int Threads = Math.Max(1, Environment.ProcessorCount - 2);
    public bool Delete = false;
    public bool Online = true;
    public void Validate()
    {
        Input = Path.GetFullPath(Input).TrimEnd('\\');
        Output = Path.GetFullPath(Output).TrimEnd('\\');
        if (!Directory.Exists(Input))
        {
            throw new Exception("Escolha uma pasta de entrada existente.");
        }

        if (Input.Length < 3 || Output.Length < 3)
        {
            throw new Exception("Use uma pasta, não a raiz de uma unidade.");
        }

        if (Input.Equals(Output, StringComparison.OrdinalIgnoreCase) || Input.StartsWith(Output + "\\",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("A saída deve ser diferente da entrada e não pode conter a pasta de entrada.");
        }

        string temp = Input + "\\temp";
        if (Output.Equals(temp, StringComparison.OrdinalIgnoreCase) || Output.StartsWith(temp + "\\",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("A saída não pode ficar na pasta temporária da entrada.");
        }

        foreach (string path in new[]{Input, Output})
        {
            for (DirectoryInfo d = new DirectoryInfo(path); d != null; d = d.Parent)
            {
                if (d.Exists && (d.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new Exception("Escolha pastas sem links ou junctions.");
                }
            }
        }

        if (Platform != "PS1" && Platform != "PS2")
        {
            throw new Exception("Plataforma inválida.");
        }

        CheckCodecs(Cd, new[]{"cdlz", "cdzs", "cdzl", "cdfl"});
        CheckCodecs(Dvd, new[]{"lzma", "zstd", "zlib", "flac", "huff"});
        if (CdHunk < 2448 || CdHunk > 1048576 || CdHunk % 2448 != 0 || DvdHunk < 2048 || DvdHunk > 1048576
            || DvdHunk % 2048 != 0)
        {
            throw new Exception("Hunk inválido para a mídia.");
        }

        if (Threads < 1 || Threads > Environment.ProcessorCount)
        {
            throw new Exception("Quantidade de threads inválida.");
        }
    }

    static void CheckCodecs(string codecs, string[] allowed)
    {
        var set = new HashSet<string>();
        foreach (string c in codecs.Split(','))
        {
            if (Array.IndexOf(allowed, c) < 0 || !set.Add(c))
            {
                throw new Exception("Selecione codecs válidos, sem repetição.");
            }
        }

        if (set.Count > 4)
        {
            throw new Exception("Selecione no máximo quatro codecs por mídia.");
        }
    }
}
