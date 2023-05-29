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
    public int Length => (int?)_file?.Length ?? 0;


    bool IsSymLink { get; set; }
    string OriginalUri { get; set; }

    ILog Logger { get; }

    public FileStorageHandler(ILog logger, AdaptorSettings settings, bool isSymLink, string originalUri)
    {
        Logger = logger;
        Settings = settings;
        _directory = new DirectoryInfo(settings.StoragePath);
        IsSymLink = isSymLink;
        OriginalUri = originalUri;
    }

    bool Exclusive => Flags.HasFlag(OpenFlags.Exclusive);
    bool Create => Flags.HasFlag(OpenFlags.Create);
    bool Folder => Flags.HasFlag(OpenFlags.Directory);
    bool ReadWrite => Flags.HasFlag(OpenFlags.ReadWrite);
    bool ReadOnly => Flags.HasFlag(OpenFlags.ReadOnly);
    bool ReadWriteProtect => Flags.HasFlag(OpenFlags.ReadWriteProtect);
    bool Writable => ReadWrite || ReadWriteProtect;

    public Task<(bool, string, int, NHACPError)> Open(OpenFlags flags, string uri)
    {
        Flags = flags;
        Path = uri;

        var exists = File.Exists(Path);
        if (Exclusive && exists) 
        {
            return Task.FromResult((false, "Exists", Length, NHACPError.Exists));
        }
        else if (!Folder && (!exists && !Create))
        {
            return Task.FromResult((false, "Not found", Length, NHACPError.NotFound));
        }
        
        
        //Logger.Write($"Create: {create}, ReadWrite: {readWrite}");
        if (Folder is false && (exists || Create))
        {
            _file = new FileInfo(Path);
            if (ReadWrite && _file.IsReadOnly)
            {
                return Task.FromResult((false, "Read Only", Length, NHACPError.AccessDenied));
            }

            _stream = new FileStream(
                _file!.FullName,
                Create ? FileMode.OpenOrCreate : FileMode.Open,
                Writable ? FileAccess.ReadWrite : FileAccess.Read, 
                FileShare.ReadWrite
            );

            if (!_file.Exists)
            {
                return Task.FromResult((false, "Cant Open", Length, NHACPError.NotPermitted));
            }
            //Length = (int)_file.Length;
            return Task.FromResult((true, string.Empty, Length, NHACPError.Undefined));
        }
        else if (Folder && Directory.Exists(Path)) {
            _directory = new DirectoryInfo(Path);
            return Task.FromResult((true, string.Empty, Length, NHACPError.Undefined));
        }
        return Task.FromResult((false, "Not found", Length, NHACPError.NotFound));
    }

    public async Task<(bool, string, Memory<byte>, NHACPError)> Get(int offset, int length, bool realLength = false)
    {
        if (_stream is null)
            return (false, string.Empty, Array.Empty<byte>(), NHACPError.InvalidRequest);
        
        if (offset > _stream.Length)  
            return (true, string.Empty, Array.Empty<byte>(), 0);
        
        var buffer = new Memory<byte>(new byte[length]);

        Logger.WriteVerbose($"Reading {length} bytes from {offset}, File Length: {_stream.Length}");
        _stream.Seek(offset, SeekOrigin.Begin);
        var read = await _stream.ReadAsync(buffer);
        if (read != length && realLength) 
            buffer = buffer[0..read];

        return (true, string.Empty, buffer, 0);
        
    }

    public async Task<(bool, string, NHACPError)> Put(int offset, Memory<byte> buffer)
    {
        if (_stream is null) 
            return (false, string.Empty, NHACPError.InvalidRequest);
        
        if (ReadWriteProtect && _file?.IsReadOnly is true)
            return (false, "Write Protected", NHACPError.WriteProtected);
        else if (ReadOnly && !ReadWriteProtect && _file?.IsReadOnly is true)
            return (false, "Read Only", NHACPError.NotPermitted);

        if (IsSymLink && Settings.EnableCopyOnSymLinkWrite)
        {
            if (Path is null || _stream is null)
            {
                return (false, "Not Open", NHACPError.NotPermitted);
            }
            _stream.Close();
            _stream.Dispose();
            Logger.Write($"Copying SymLink target to `{OriginalUri}`");
            File.Copy(Path, OriginalUri, true);

            Path = OriginalUri;
            IsSymLink = false;

            _file = new FileInfo(Path);
            _stream = new FileStream(
                _file!.FullName,
                FileMode.Open,
                Writable ? FileAccess.ReadWrite : FileAccess.Read,
                FileShare.ReadWrite
            );
            //length = (int)_file.Length;
        }
        else if (IsSymLink)
        {
            Logger.WriteWarning($"SymLinks are write-protected: `{Path}`");
            return (false, "SymLink", NHACPError.WriteProtected);
        }

        _stream!.Seek(offset, SeekOrigin.Begin);
        await _stream!.WriteAsync(buffer);
        return (true, string.Empty, 0);
    }

    public void End()
    {
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


    public async Task<(bool, string, Memory<byte>, NHACPError)> Read(int length)
    {
        if (Position > Length) return (true, string.Empty, Array.Empty<byte>(), NHACPError.Undefined);
        if ((Position + length) > Length) length = Length - Position;

        var result = await Get(Position, length, true);
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
        
        pattern = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;
        _list.Clear();
        _listPosition = 0;

        var r = NabuLibEx.List(_directory.FullName, Settings, pattern);

        _list.AddRange(r);
        return (true, string.Empty, 0);
    }

    public (bool, string?, string, NHACPError) GetDirEntry(byte maxNameLength)
    {
        if (_listPosition == _list.Count)
            return (true, null, string.Empty, 0);
        var entry = _list[_listPosition++];
        return (true, entry, string.Empty, 0);
    }


    public Task Close()
    {
        _file = null;
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
