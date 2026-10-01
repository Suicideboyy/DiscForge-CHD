
sealed partial class ConversionSession
{
    // Listing validates archive paths without decompressing images or trusting their console type.
    internal async Task PreviewArchiveAsync(string source, string destination, long packed)
    {
        var files = await archiveReader.ListAsync(source, destination).ConfigureAwait(false);
        var serials = files.Select(f => MediaFiles.NormalizeSerial(Path.GetFileName(f.Path)))
            .Append(MediaFiles.NormalizeSerial(Path.GetFileName(source)))
            .Where(s => s.Length > 0).Distinct().ToArray();
        var game = new GameInfo
        {
            Source = source, Image = source, Title = MediaFiles.OutputStem(source),
            SourceFormat = Path.GetExtension(source).TrimStart('.').ToUpperInvariant(),
            CurrentFormat = Path.GetExtension(source).TrimStart('.').ToUpperInvariant(),
            SourceSize = packed, CurrentSize = packed, Status = "IDENTIFYING"
        };
        Show(game);
        var match = serials.Length > 1 ? null : await database.LookupPreviewAsync(
            serials.FirstOrDefault() ?? "", game.Title).ConfigureAwait(false);
        if (match != null)
        {
            game.Title = match.Value.Record.Title;
            game.Serial = match.Value.Record.Serial;
            game.System = match.Value.System;
            game.Type = match.Value.Record.Type;
            game.DatabaseUrl = match.Value.Record.Url;
        }
        game.Status = "EXTRACTING";
        Show(game);
    }

    GameInfo Identify(string source, DiscInput media)
    {
        var game = new GameInfo
        {
            Source = source,
            Image = media.Path,
            Title = MediaFiles.OutputStem(source),
            SourceFormat = Path.GetExtension(source).TrimStart('.').ToUpperInvariant(),
            SourceSize = new FileInfo(source).Length,
            CurrentFormat = Path.GetExtension(media.Path).TrimStart('.').ToUpperInvariant(),
            CurrentSize = new FileInfo(media.Path).Length
        };
        game.Serial = MediaFiles.NormalizeSerial(Path.GetFileName(media.Path));
        if (game.Serial.Length == 0 && media.CueText != null)
        {
            game.Serial = MediaFiles.NormalizeSerial(media.CueText);
        }

        if (game.Serial.Length == 0)
        {
            game.Serial = MediaFiles.NormalizeSerial(Path.GetFileName(source));
        }

        return game;
    }

    void Enrich(GameInfo game)
    {
        game.Status = "IDENTIFYING";
        Show(game);
        GameRecord hit = game.System == "PS2" ? database.Lookup(game.Serial)
            : database.LookupPs1(game.Serial, game.Title);
        game.Lookup = hit != null ? "Serial confirmed / " + hit.Provider
            : !_settings.Online ? "Online lookup disabled" : "No unambiguous database match";
        if (hit != null)
        {
            if (!String.IsNullOrWhiteSpace(hit.Title)) game.Title = hit.Title;
            game.Type = hit.Type;
            game.DatabaseUrl = hit.Url;
        }

        game.Status = "PROCESSING";
        Show(game);
    }

    async Task<bool> ValidExisting(string path)
    {
        FileSystemPaths.EnsureNoLinks(path);
        if (!File.Exists(path))
        {
            return false;
        }

        var result = await toolRunner.RunAsync("chdman.exe", new[]{"info", "-i", path},
            false).ConfigureAwait(false);
        if (result.Code != 0)
        {
            throw new IOException("Existing CHD is invalid and was preserved for review: " + path);
        }

        return true;
    }

}
