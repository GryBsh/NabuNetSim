using System;
using System.Security;
using Microsoft.Extensions.FileSystemGlobbing;
using Nabu.Services;

namespace Nabu.Network.NHACP.V01;

public class FileStorageHandler : INHACPStorageHandler
{
    AdaptorSettings Settings;
    String? Path;
    OpenFlags Flags;
    FileInfo? _file;
    FileStream? _stream;
    DirectoryInfo? _directory;
    readonly List<string> _list = new();
    int _listPosition = 0;
    public int Position { get; private set; }
    public int Length { get; private set; }

    IConsole Logger { get; }

    public FileStorageHandler(IConsole logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
        _directory = new DirectoryInfo(settings.StoragePath);
    }

    public Task<(bool, string, int, NHACPError)> Open(OpenFlags flags, string uri)
    {
        Flags = flags;
        Path = NabuLib.FilePath(Settings, uri);

        var create = Flags.HasFlag(OpenFlags.Create);
        var readWrite = (
            Flags.HasFlag(OpenFlags.ReadWrite) || Flags.HasFlag(OpenFlags.ReadWriteProtect)
        );
        //Logger.Write($"Create: {create}, ReadWrite: {readWrite}");
        if (File.Exists(Path))
        {
            _file = new FileInfo(Path);
            _stream = new FileStream(
                _file!.FullName,
                create ? FileMode.OpenOrCreate : FileMode.Open,
                readWrite ? FileAccess.ReadWrite : FileAccess.Read, 
                FileShare.ReadWrite
            );
            Length = _file.Exists ? (int)_file.Length : 0;
            return Task.FromResult((true, string.Empty, Length, NHACPError.Undefined));
        }
        else if (Directory.Exists(Path)) {
            _directory = new DirectoryInfo(Path);
            return Task.FromResult((true, string.Empty, Length, NHACPError.Undefined));
        }
        return Task.FromResult((false, "Not found", Length, NHACPError.NotFound));
    }

    public async Task<(bool, string, byte[], NHACPError)> Get(int offset, int length)
    {
        if (_stream is null)
            return (false, string.Empty, Array.Empty<byte>(), NHACPError.InvalidRequest);

        var buffer = new byte[length];
            
        var end = offset + length;
            
        if (end > _stream.Length) length = (short)(_stream.Length - offset);

        Logger.WriteVerbose($"Reading {length} bytes from {offset}, File Length: {_stream.Length}");
        _stream.Seek(offset, SeekOrigin.Begin);
        await _stream.ReadAsync(buffer.AsMemory(0, length));

        return (true, string.Empty, buffer, 0);
        
    }

    public async Task<(bool, string, NHACPError)> Put(int offset, Memory<byte> buffer)
    {
        if (_stream is null) 
            return (false, string.Empty, NHACPError.InvalidRequest);
        
        if (Flags.HasFlag(OpenFlags.ReadWriteProtect))
            return (false, "Write Protected", NHACPError.NotPermitted);
        _stream!.Seek(offset, SeekOrigin.Begin);
        await _stream!.WriteAsync(buffer);
        return (true, string.Empty, 0);
    }

    public void End()
    {
        _file = null;
        Close();
    }

    public (bool, int, string, NHACPError) Seek(int offset, NHACPSeekOrigin origin)
    {
        if (_stream is null)
            return (false, 0, string.Empty, NHACPError.InvalidRequest);

        int? newPosition = origin switch
        {
            NHACPSeekOrigin.Set     => offset,
            NHACPSeekOrigin.Current => Position + offset,
            NHACPSeekOrigin.End     => Length - offset,
            _                       => null
        };

        if (newPosition is null) return (false, Position, "Unknown Seek Origin", NHACPError.InvalidRequest);

        _stream.Seek((long)newPosition!, SeekOrigin.Begin);
        
        return (true, Position, string.Empty, NHACPError.Undefined);
    }


    public async Task<(bool, string, byte[], NHACPError)> Read(int length)
    {
        if (Position > Length) return (true, string.Empty, Array.Empty<byte>(), NHACPError.Undefined);
        if ((Position + length) > Length) length = Length - Position;

        var result = await Get(Position, length);
        Position += length;
        return result;
    }

    public async Task<(bool, string, NHACPError)> Write(Memory<byte> buffer)
    {
        if (Position > Length)
        {
            var fillLength = Position - Length;
            await Put(Length, Enumerable.Repeat((byte)0x00, fillLength).ToArray());
        }
        var result = await Put(Position, buffer);
        
        return result;
    }

    public (bool, string, NHACPError) ListDir(string pattern)
    {
        if (_directory is null) return (false, string.Empty, NHACPError.InvalidRequest);

        _list.Clear();
        _listPosition = 0;

        Matcher matcher = new();
        matcher.AddInclude(pattern);
        var r = matcher.GetResultsInFullPath(_directory.FullName);

        _list.AddRange(r);
        return (true, string.Empty, 0);
    }

    public (bool, string, string, NHACPError) GetDirEntry(byte maxNameLength)
    {
        var entry = _list[_listPosition++];
        return (true, entry, string.Empty, 0);
    }


    public Task Close()
    {
        _list.Clear();
        _stream?.Dispose();
        return Task.CompletedTask;
    }

    public (bool, string, string, NHACPError) Info()
    {
        if (Path is null) return (false, string.Empty, "No file/directory open", NHACPError.InvalidRequest);
        return (true, Path, string.Empty, 0);
    }

    public (bool, int, string, NHACPError) SetSize(int size)
    {
        _stream?.SetLength(size);
        return (true, (int)(_stream?.Length ?? 0), string.Empty, 0);   
    }
}
