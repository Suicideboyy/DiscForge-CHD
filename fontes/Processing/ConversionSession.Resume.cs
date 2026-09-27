using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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

        if (_settings.Platform == "PS2")
        {
            // Start loading the cover while the media lookup is still running.
            game.Status = "IDENTIFYING";
            Show(game);
            GameRecord hit = database.Lookup(game.Serial);
            game.Lookup = hit == null ? (_settings.Online
                ? "Database unavailable or no unambiguous match" : "Online lookup disabled")
                : "Serial confirmed / " + hit.Provider;
            if (hit != null)
            {
                if (!String.IsNullOrWhiteSpace(hit.Title))
                {
                    game.Title = hit.Title;
                }

                game.Type = hit.Type;
                game.DatabaseUrl = hit.Url;
            }
        }
        else
        {
            game.Lookup = "PS1 platform";
        }

        game.Status = "PROCESSING";
        Show(game);
        return game;
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

    async Task<bool> Already(string source, List<DiscInput> media)
    {
        if (media.Count == 0 || media.Any(m => MediaFiles.TrackGroup(m.Path) != null))
        {
            return false;
        }

        var used = new HashSet<string>(Paths);
        var games = new List<GameInfo>();
        for (int i = 0; i < media.Count; i++)
        {
            var game = Identify(source, media[i]);
            string output = outputNaming.Build(game, source, media[i], i, media.Count);
            if (!used.Add(output) || !await ValidExisting(output).ConfigureAwait(false))
            {
                return false;
            }

            game.Status = "ALREADY EXISTS";
            games.Add(game);
        }

        foreach (var game in games)
        {
            Show(game);
        }

        Say("Already exists: " + Path.GetFileName(source) + " — extraction skipped.");
        return true;
    }
}
