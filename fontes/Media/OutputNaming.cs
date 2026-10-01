
sealed class OutputNaming(string outputDirectory)
{
    public string Build(GameInfo g, string source, DiscInput media, int index, int total)
    {
        string name = g.Title;
        string revision = SourceRevision(source);
        if (revision.Length > 0 && !Regex.IsMatch(name, @"(?i)(?:^|\s)[\[(]?" +
            Regex.Escape(revision) + @"[\])]?(?:\s|$)"))
            name += " [" + revision + "]";
        if (name.Length > 120)
            name = name.Substring(0, 120).TrimEnd(' ', '.');

        if (!String.IsNullOrEmpty(g.Serial))
        {
            name = Regex.Replace(name, @"\s*\[" + Regex.Escape(g.Serial) + @"\]", "");
            name += " [" + g.Serial + "]";
        }

        if (total > 1)
        {
            name += " - Disco " + (index + 1).ToString("D2");
            if (String.IsNullOrEmpty(g.Serial) && g.Title == MediaFiles.OutputStem(source))
                name += " - " + Path.GetFileNameWithoutExtension(media.Path);
        }

        name = Regex.Replace(Regex.Replace(name, @"[<>:""/\\|?*\x00-\x1F]", " - "), @"\s+",
            " ").Trim().TrimEnd('.');
        if (name.Length > 180)
            name = name.Substring(0, 180).TrimEnd(' ', '.');

        if (Regex.IsMatch(name, @"(?i)^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)"))

            name = "_" + name;

        return Path.Combine(outputDirectory, name + ".chd");
    }

    // The revision in the archive name distinguishes dumps sharing a Redump serial.
    public static string SourceRevision(string source)
    {
        string stem = MediaFiles.OutputStem(source);
        var match = Regex.Match(stem, @"(?i)(?<![a-z0-9])(?:v(?:er(?:sion)?)?|rev(?:ision)?)\.?\s*[-_ ]*(\d+(?:[._]\d+)*)\b");
        if (!match.Success)
            return "";

        string digits = match.Groups[1].Value.Replace('_', '.');
        return match.Value.TrimStart('(', '[', ' ').StartsWith("rev", StringComparison.OrdinalIgnoreCase)
            ? "Rev " + digits : "v" + digits;
    }
}
