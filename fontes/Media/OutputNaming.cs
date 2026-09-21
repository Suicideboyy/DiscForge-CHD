using System;
using System.IO;
using System.Text.RegularExpressions;

sealed class OutputNaming
{
    readonly string outputDirectory;
    public OutputNaming(string outputDirectory)
    {
        this.outputDirectory = outputDirectory;
    }

    public string Build(GameInfo g, string source, DiscInput media, int index, int total)
    {
        string name = g.Title;
        if (name.Length > 120)
        {
            name = name.Substring(0, 120).TrimEnd(' ', '.');
        }

        if (!String.IsNullOrEmpty(g.Serial))
        {
            name = Regex.Replace(name, @"\s*\[" + Regex.Escape(g.Serial) + @"\]", "");
            name += " [" + g.Serial + "]";
        }

        if (total > 1)
        {
            name += " - Disco " + (index + 1).ToString("D2");
            if (String.IsNullOrEmpty(g.Serial) && g.Title == MediaFiles.OutputStem(source))
            {
                name += " - " + Path.GetFileNameWithoutExtension(media.Path);
            }
        }

        name = Regex.Replace(Regex.Replace(name, @"[<>:""/\\|?*\x00-\x1F]", " - "), @"\s+",
            " ").Trim().TrimEnd('.');
        if (name.Length > 180)
        {
            name = name.Substring(0, 180).TrimEnd(' ', '.');
        }

        if (Regex.IsMatch(name, @"(?i)^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)"))
        {
            name = "_" + name;
        }

        return Path.Combine(outputDirectory, name + ".chd");
    }
}
