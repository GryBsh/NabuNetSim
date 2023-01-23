using Microsoft.Extensions.Logging;
using Nabu.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Network;

public class FileProgramRetriever : ProgramRetriever
{
    public FileProgramRetriever(ILogger<ProgramRetriever> logger)
    {
        Logger = logger;
    }

    public ILogger<ProgramRetriever> Logger { get; }

    #region File
    public async Task<byte[]> GetPakBytes(string path, int pak, bool encrypted = true)
    {
        var filename = NabuLib.PakName(pak);
        path = Path.Join(path, $"{filename}.npak");
        var npak = await File.ReadAllBytesAsync(path);
        //Trace($"NPAK Length: {npak.Length}");
        npak = NabuLib.Unpak(npak);
        //Trace($"Segment Length: {npak.Length}");
        return npak;
    }

    public async Task<byte[]> GetRawBytes(string path, int pak, string? image = null)
    {
        var filename = image switch
        {
            null => NabuLib.FormatTriple(pak),
            not null when pak is 1 => image,
            _ => NabuLib.FormatTriple(pak)
        };

        path = Path.Join(path, $"{filename}.nabu");
        var buffer = await File.ReadAllBytesAsync(path);
        return buffer;
    }

    public IEnumerable<NabuProgram> GetImageList(string sourceName, string path)
    {

        if (path is null) yield break;

        var files = Directory.GetFiles(path, "*.npak");
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            yield return new(
                name,
                name,
                sourceName,
                DefinitionType.Folder,
                file,
                SourceType.Local,
                ImageType.Pak,
                new[] { new PassThroughPatch(Logger) }
            );
        }

        files = Directory.GetFiles(path, "*.nabu");
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            yield return new(
                name,
                name,
                sourceName,
                DefinitionType.Folder,
                file,
                SourceType.Local,
                ImageType.Raw,
                new[] { new PassThroughPatch(Logger) }
            );
        }
    }

    #endregion

}
