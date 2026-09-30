using System;
using System.IO;
using System.Threading.Tasks;

sealed partial class ConversionSession
{
    GameInfo Identify(string source, DiscInput media)
    {
        var game = new GameInfo
        {
            Source = source,
            Image = media.Path,
            Title = MediaFiles.OutputStem(source)
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
        if (game.System == "PS2")
        {
            GameRecord hit = database.Lookup(game.Serial);
            game.Lookup = hit == null ? (_settings.Online
                ? "Database unavailable or no unambiguous match" : "Online lookup disabled")
                : "Serial confirmed / " + hit.Provider;
            if (hit != null)
            {
                if (!String.IsNullOrWhiteSpace(hit.Title)) game.Title = hit.Title;
                game.Type = hit.Type;
                game.DatabaseUrl = hit.Url;
            }
        }
        else
        {
            GameRecord hit = database.LookupPs1(game.Serial, game.Title);
            game.Lookup = hit == null ? "PS1 disc header" : "Serial confirmed / " + hit.Provider;
            if (hit != null)
            {
                if (!String.IsNullOrWhiteSpace(hit.Title)) game.Title = hit.Title;
                game.Type = "CD";
                game.DatabaseUrl = hit.Url;
            }
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
