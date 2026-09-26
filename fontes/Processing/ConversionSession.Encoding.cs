using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

sealed partial class ConversionSession
{
    // Publica a saída somente depois de codificar e verificar sua integridade.
    async Task<string> Convert(string source, DiscInput media, string root, int index, int total,
        long packed, HashSet<string> outputs)
    {
        var game = Identify(source, media);
        try
        {
            if (MediaFiles.TrackGroup(media.Path) != null)
            {
                throw new IOException("CUE AUSENTE: forneça o CUE original para o conjunto de faixas BIN.");
            }

            string output = outputNaming.Build(game, source, media, index, total);
            if (!outputs.Add(output))
            {
                throw new IOException("Dois discos resultaram no mesmo nome de saída.");
            }

            if (await ValidExisting(output).ConfigureAwait(false))
            {
                game.Status = "JÁ EXISTENTE";
                Show(game);
                Say("Já existente: " + Path.GetFileName(output));
                return null;
            }

            string input = media.Path;
            bool chd = MediaFiles.Extension(input) == ".chd";
            long original = new FileInfo(input).Length, oldHunk = 0;
            string folder = Path.Combine(_workingDirectory, "disc-" + (index + 1));
            Directory.CreateDirectory(folder);
            if (chd)
            {
                string info = await toolRunner.RequireSuccessAsync("chdman.exe", false, "info", "-i",
                    input).ConfigureAwait(false);
                oldHunk = MediaFiles.ReadInfoNumber(info, "Hunk Size");
                long unit = MediaFiles.ReadInfoNumber(info, "Unit Size");
                game.Type = unit == 2448 || Regex.IsMatch(info, @"CHT2|CHTR|CHCD") ? "CD" : unit == 2048
                    || info.Contains("DVD ") ? "DVD" : "";
                if (game.Type.Length == 0)
                {
                    throw new IOException("Tipo de CHD não reconhecido como CD/DVD.");
                }

                game.Detection = "Metadados CHD";
                Say("Extraindo: " + Path.GetFileName(input));
                if (game.Type == "CD")
                {
                    input = Path.Combine(folder, "disc.cue");
                    await toolRunner.RequireSuccessAsync("chdman.exe", true, "extractcd", "-i", media.Path,
                        "-o", input, "-ob", Path.Combine(folder, "disc.bin")).ConfigureAwait(false);
                }
                else
                {
                    input = Path.Combine(folder, "disc.iso");
                    await toolRunner.RequireSuccessAsync("chdman.exe", true, "extractdvd", "-i", media.Path,
                        "-o", input).ConfigureAwait(false);
                }
            }
            else if (MediaFiles.Extension(input) == ".cue")
            {
                var refs = MediaFiles.ReadCueReferences(media.CueText, input, root);
                original = 0;
                foreach (string p in refs)
                {
                    FileSystemPaths.EnsureNoLinks(p);
                    var f = new FileInfo(p);
                    if (!f.Exists || f.Length == 0)
                    {
                        throw new IOException("Faixa ausente/vazia: " + p);
                    }

                    original += f.Length;
                }

                game.Type = "CD";
                game.Detection = "Descritor CUE original";
            }
            else
            {
                byte[] header = new byte[16];
                using (var stream = File.OpenRead(input))
                {
                    stream.ReadExactly(header);
                }

                bool raw = original > 0 && original % 2352 == 0 && header[0] == 0 && header[11] == 0
                    && header.Skip(1).Take(10).All(b => b == 255) && (header[15] == 1 || header[15] == 2);
                string mode;
                if (raw)
                {
                    game.Type = "CD";
                    mode = "MODE" + header[15] + "/2352";
                    game.Detection = "Cabeçalho RAW de CD: " + mode;
                }
                else if (original > 0 && original % 2048 == 0)
                {
                    mode = "MODE1/2048";
                    if (game.Type.Length == 0)
                    {
                        game.Type = original <= 400L * 1024 * 1024 ? "CD" : "DVD";
                        game.Detection = "Heurística: imagem de 2048 bytes/setor; limite de 400 MiB";
                    }
                    else
                    {
                        game.Detection = "Serial " + game.Serial + " / " + game.Lookup;
                    }
                }
                else
                {
                    throw new IOException(("Imagem sem setores de 2048/2352 reconhecíveis. Para BIN "
                        + "multifaixa/áudio, forneça o CUE original."));
                }

                if (game.Type == "CD")
                {
                    string relative = Uri.UnescapeDataString(new Uri(folder
                        + "\\").MakeRelativeUri(new Uri(input)).ToString()).Replace('/', '\\');
                    input = Path.Combine(folder, "disc.cue");
                    File.WriteAllText(input, "FILE \"" + relative + "\" BINARY\r\n  TRACK 01 " + mode
                        + "\r\n    INDEX 01 00:00:00\r\n", new UTF8Encoding(false));
                }
            }

            if (_settings.Platform == "PS1" && game.Type != "CD")
            {
                throw new IOException("A plataforma PS1 aceita apenas CHDs de CD.");
            }

            int hunk = game.Type == "CD" ? _settings.CdHunk : _settings.DvdHunk;
            string codecs = game.Type == "CD" ? _settings.Cd : _settings.Dvd;
            Say("Tipo: " + game.Type + " | Hunk: " + hunk + " | Codecs: " + codecs);
            Show(game);
            string candidate = Path.Combine(folder, "encoded.chd");
            Say("Comprimindo: " + game.Title);
            await toolRunner.RequireSuccessAsync("chdman.exe", true, game.Type == "CD" ? "createcd"
                : "createdvd", "-i", input, "-o", candidate, "-hs", hunk.ToString(), "-c", codecs, "-np",
                _settings.Threads.ToString()).ConfigureAwait(false);
            if (chd && new FileInfo(media.Path).Length <= new FileInfo(candidate).Length
                && (_settings.Platform == "PS1" || oldHunk == hunk))
            {
                candidate = media.Path;
                Say("Original CHD menor: mantido.");
            }

            // Copy into the destination volume, verify there, then publish without overwrite.
            string pending = Path.Combine(_settings.Output, ".chdopt-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.Copy(candidate, pending, false);
                Say("Verificando: " + game.Title);
                await toolRunner.RequireSuccessAsync("chdman.exe", true, "verify", "-i",
                    pending).ConfigureAwait(false);
                FileSystemPaths.EnsureNoLinks(output);
                File.Move(pending, output);
            }
            finally
            {
                if (File.Exists(pending))
                {
                    File.Delete(pending);
                }
            }

            long final = new FileInfo(output).Length;
            Say("Salvo: " + output + " | Original: " + original + " bytes | CHD: " + final + " bytes"
                + (packed > 0 ? " | Compactado: " + packed + " bytes" : ""));
            game.Status = "CONCLUÍDO";
            game.Detail = "CHD verificado; " + final + " bytes";
            Show(game);
            return output;
        }
        catch (Exception ex)
        {
            game.Status = "ERRO";
            game.Detail = ex.Message;
            Show(game);
            throw;
        }
    }
}
