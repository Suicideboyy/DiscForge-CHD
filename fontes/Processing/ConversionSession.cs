using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

sealed partial class ConversionSession
{
    readonly EncoderSettings _settings;
    readonly Action<string> _report;
    readonly string _temporaryDirectory;
    readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
    readonly object _logLock = new object ();
    int _successfulEntries;
    int _existingEntries;
    int _failedEntries;
    string _workingDirectory;
    static readonly StringComparer Paths = StringComparer.OrdinalIgnoreCase;
    readonly ToolRunner toolRunner;
    readonly ArchiveReader archiveReader;
    readonly GameDatabase database;
    readonly OutputNaming outputNaming;
    public ConversionSession(EncoderSettings settings, Action<string> report, string temporaryDirectory)
    {
        _settings = settings;
        _report = report;
        _temporaryDirectory = temporaryDirectory;
        toolRunner = new ToolRunner(Say, () => _workingDirectory ?? _temporaryDirectory);
        archiveReader = new ArchiveReader(toolRunner);
        database = new GameDatabase(settings.Online, temporaryDirectory, Say);
        outputNaming = new OutputNaming(settings.Output);
    }

    IEnumerable<string> Sources(string root)
    {
        foreach (string f in Directory.GetFiles(root))
        {
            FileSystemPaths.EnsureNoLinks(f);
            yield return f;
        }

        if (_settings.Platform == "PS1")
        {
            yield break;
        }

        foreach (string d in Directory.GetDirectories(root))
        {
            if (Paths.Equals(d, _settings.Output) || FileSystemPaths.IsInside(d, _settings.Output)
                || Paths.Equals(d, _temporaryDirectory))
            {
                continue;
            }

            FileSystemPaths.EnsureNoLinks(d);
            foreach (string f in Sources(d))
            {
                yield return f;
            }
        }
    }

    public async Task<int> Run()
    {
        Say(AppInfo.DisplayName + " — " + DateTime.Now);
        Say("Origem: " + _settings.Input + " | Saída: " + _settings.Output);
        var sources = Sources(_settings.Input).ToList();
        var images = MediaFiles.SelectDiscInputs(sources.Where(p => _settings.Platform == "PS2"
            ? MediaFiles.IsDiscImage(p) : MediaFiles.Extension(p) == ".chd"), _settings.Input, null);
        var entries = images.Select(m => m.Path).Concat(_settings.Platform == "PS2"
            ? sources.Where(MediaFiles.IsArchive)
            : Enumerable.Empty<string>()).OrderBy(MediaFiles.NaturalSortKey, Paths).ToList();
        for (int i = 0; i < entries.Count; i++)
        {
            if (File.Exists(Engine.StopFile))
            {
                return 2;
            }

            string source = entries[i];
            _workingDirectory = Path.Combine(_temporaryDirectory, "native-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingDirectory);
            Say("Entrada " + (i + 1) + " / " + entries.Count + ": " + Path.GetFileName(source));
            try
            {
                if (MediaFiles.IsArchive(source))
                {
                    var snapshots = ArchiveReader.GetVolumes(source).Select(p => new FileSnapshot(p)).ToList();
                    long packed = snapshots.Sum(p => p.Size);
                    string unpacked = Path.Combine(_workingDirectory, "unpacked");
                    var listed = await archiveReader.ListAsync(source, unpacked).ConfigureAwait(false);
                    var preview = await archiveReader.PreviewAsync(source, unpacked,
                        listed).ConfigureAwait(false);
                    if (await Already(source, preview).ConfigureAwait(false))
                    {
                        _existingEntries++;
                        continue;
                    }

                    // Reject orphan tracks before extraction, including old partial CHDs.
                    var orphan = preview.FirstOrDefault(m => MediaFiles.TrackGroup(m.Path) != null);
                    if (orphan != null)
                    {
                        var game = Identify(source, orphan);
                        game.Type = "CD";
                        game.Status = "CUE AUSENTE";
                        game.Detail = "Faixas BIN pertencem ao mesmo disco. Forneça o CUE original.";
                        Show(game);
                        throw new IOException(game.Detail);
                    }

                    Say("Descompactando: " + Path.GetFileName(source));
                    Directory.CreateDirectory(unpacked);
                    await toolRunner.RequireSuccessAsync("7z.exe", true, "x", "-y", "-p", "-bsp1",
                        "-sccUTF-8", "-o" + unpacked, "--", source).ConfigureAwait(false);
                    var files = FileSystemPaths.EnumerateFiles(unpacked).ToList();
                    if (files.Count == 1 && MediaFiles.Extension(files[0]) == ".tar")
                    {
                        string nested = Path.Combine(_workingDirectory, "tar");
                        await archiveReader.ListAsync(files[0], nested).ConfigureAwait(false);
                        Directory.CreateDirectory(nested);
                        await toolRunner.RequireSuccessAsync("7z.exe", true, "x", "-y", "-p", "-bsp1", "-o"
                            + nested, "--", files[0]).ConfigureAwait(false);
                        unpacked = nested;
                        files = FileSystemPaths.EnumerateFiles(unpacked).ToList();
                    }

                    var media = MediaFiles.SelectDiscInputs(files, unpacked, null);
                    if (media.Count == 0)
                    {
                        throw new IOException("Nenhuma imagem de disco encontrada.");
                    }

                    bool allNew = true;
                    var saved = new List<FileSnapshot>();
                    var outputs = new HashSet<string>(Paths);
                    for (int d = 0; d < media.Count; d++)
                    {
                        var result = await Convert(source, media[d], unpacked, d, media.Count, packed,
                            outputs).ConfigureAwait(false);
                        if (result == null)
                        {
                            allNew = false;
                        }
                        else
                        {
                            saved.Add(new FileSnapshot(result));
                        }
                    }

                    if (allNew)
                    {
                        _successfulEntries++;
                    }
                    else
                    {
                        _existingEntries++;
                    }

                    if (_settings.Delete && allNew && saved.Count == media.Count)
                    {
                        if (!snapshots.All(p => p.Unchanged()) || !saved.All(p => p.Unchanged()))
                        {
                            throw new IOException("Arquivos mudaram durante o processamento; compactado preservado.");
                        }

                        foreach (var part in snapshots)
                        {
                            File.Delete(part.Path);
                            Say("Compactado removido após verificação: " + Path.GetFileName(part.Path));
                        }
                    }
                }
                else
                {
                    var media = images.First(m => Paths.Equals(m.Path, source));
                    var output = await Convert(source, media, _settings.Input, 0, 1, 0,
                        new HashSet<string>(Paths)).ConfigureAwait(false);
                    if (output == null)
                    {
                        _existingEntries++;
                    }
                    else
                    {
                        _successfulEntries++;
                    }
                }
            }
            catch (Exception ex)
            {
                _failedEntries++;
                Say("ERRO: " + ex.Message);
            }
            finally
            {
                try
                {
                    FileSystemPaths.DeleteWorkDirectory(_workingDirectory, _temporaryDirectory);
                }
                catch (Exception ex)
                {
                    Say("Aviso: limpeza temporária: " + ex.Message);
                }

                _workingDirectory = null;
                Say("Lote " + _settings.Platform + ": " + (100.0 * (i + 1) / Math.Max(1,
                    entries.Count)).ToString("F1") + "% (" + (i + 1) + "/" + entries.Count
                    + ") | Sucessos: " + _successfulEntries + " | Já existentes: " + _existingEntries
                    + " | Erros: " + _failedEntries);
            }
        }

        return _failedEntries == 0 ? 0 : 1;
    }
}
