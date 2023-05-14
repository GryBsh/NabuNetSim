using Nabu.Adaptor;
using Nabu.Network.NHACP.V0;
using Nabu.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Nabu.Network.NHACP.V01;


public class NHACPV01Protocol : Protocol
{
    HttpClient Http;
    FileCache FileCache;
    Dictionary<byte, NHACPV01Session> Sessions { get; }

    Dictionary<NHACPError, string> ErrorMessages { get; } = new()
    {
        [NHACPError.Undefined]          = "Unknown",
        [NHACPError.NotSupported]       = "Not Supported",
        [NHACPError.NotPermitted]       = "Not Permitted",
        [NHACPError.NotFound]           = "Not Found",
        [NHACPError.IOError]            = "IO Error",
        [NHACPError.BadDescriptor]      = "Bad Descriptor",
        [NHACPError.OutOfMemory]        = "Out of Memory",
        [NHACPError.PermissionDenied]   = "Permission Denied",
        [NHACPError.Busy]               = "Busy",
        [NHACPError.Exists]             = "Exists",
        [NHACPError.IsDirectory]        = "Is Directory",
        [NHACPError.InvalidRequest]     = "Invalid Request",
        [NHACPError.TooManyOpen]        = "Too Many Open",
        [NHACPError.TooLarge]           = "Too Large",
        [NHACPError.OutOfSpace]         = "Out of Space",
        [NHACPError.NoSeek]             = "Not Seekable",
        [NHACPError.NotADirectory]      = "Not a Directory"
    };

    public NHACPV01Protocol(IConsole<NHACPV01Protocol> logger, HttpClient http, FileCache fileCache) : base(logger)
    {
        Sessions = new();
        Http = http;
        FileCache = fileCache;
    }

    byte NextIndex(byte sessionId)
    {
        for (int i = 0x00; i < 0xFF; i++)
        {
            if (Sessions[sessionId].ContainsKey((byte)i)) continue;
            return (byte)i;
        }
        return 0xFF;
    }

    byte[] ErrorResult(byte sessionId, Exception ex)
    {
        var (code, error) = ex switch
        {
            FileNotFoundException       => (NHACPError.NotFound, ErrorMessages[NHACPError.NotFound]),
            UnauthorizedAccessException => (NHACPError.PermissionDenied, ErrorMessages[NHACPError.PermissionDenied]),
            SecurityException           => (NHACPError.PermissionDenied, ErrorMessages[NHACPError.PermissionDenied]),
            IOException                 => (NHACPError.Busy, ErrorMessages[NHACPError.Busy]),
            _                           => (NHACPError.Undefined, ErrorMessages[NHACPError.Undefined])
        };
        return ErrorResult(sessionId, code, string.IsNullOrWhiteSpace(ex.Message) ? error : ex.Message);
    }

    byte[] ErrorResult(byte sessionId, NHACPError code, string error)
    {
        Sessions[sessionId].LastError = code;
        Sessions[sessionId].LastErrorMessage = error;
        Error($"{sessionId} Error: {code}: {error}");
        return NHACPMessage.Error(code, error).ToArray();
    }

    private byte[] Hello(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, acp) = NabuLib.Slice(buffer, 1, 3, NabuLib.ToASCII);
            (i, var version) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var options) = NabuLib.Slice(buffer, i, 2, o => (NHACPOptions)NabuLib.ToShort(o));
            Log($"Starting session: {sessionId} v{version}, Options: {options}");

            var isMagic = acp is NHACPMessage.Magic;
            var supportedVersion = NHACPMessage.SupportedVersions.Contains(version);
            var validSessionId = sessionId is 0xFF or 0x00;
            if (!isMagic || !supportedVersion || !validSessionId)
            {
                Error("Invalid Start Request");
                return ErrorResult(sessionId, NHACPError.InvalidRequest, "Open request is invalid");
            }
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }

        if (Sessions.TryGetValue(sessionId, out var session))
        {
            try { session.Dispose(); } catch { }
        }

        Sessions[sessionId] = new();

        SendCRC = Options.HasFlag(NHACPOptions.CRC8);
        Log("Started");
        return NHACPMessage.NHACPStarted(sessionId, CurrentVersion, Emulator.Id).ToArray();
    }

    public async Task<byte[]> StorageOpen(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var flags) = NabuLib.Slice(buffer, i, 2, o => (OpenFlags)NabuLib.ToShort(o));
            (i, var length) = NabuLib.Pop(buffer, i);
            (i, var uri) = NabuLib.Slice(buffer, i, length, NHACPStructure.String);
            if (index is 0xFF) index = NextIndex(sessionId);

            Log($"{sessionId} Open: {index}, Flags: {flags}, Uri: {uri} ({length} bytes)");
                      
            Sessions[sessionId][index] = uri switch
            {
                var path when path.StartsWith("http") || path.StartsWith("https")
                            => new HttpStorageHandler(Logger, Settings, Http, FileCache),
                var path when path.StartsWith("0x")
                            => new RAMStorageHandler(Logger, Settings),
                _ => new FileStorageHandler(Logger, Settings)
            };

            var (success, error, size, code) = await Sessions[sessionId][index].Open(flags, uri);

            if (success) return NHACPMessage.StorageLoaded(index, size).ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex) {
            return ErrorResult(sessionId, ex);
        }

        
    }

    public async Task<byte[]> StorageGet(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var offset) = NabuLib.Pop(buffer, i);
            (i, var length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);

            Log($"{sessionId} GET: {index}, Offset: {offset}, Length: {length}");
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, error, data, code) = await Sessions[sessionId][index].Get(offset, length);

            if (success) return NHACPMessage.Buffer(data).ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }

    }
    public async Task<byte[]> StoragePut(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var offset) = NabuLib.Pop(buffer, i);
            (i, var length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var data) = NabuLib.Slice(buffer, i, length);

            Log($"{sessionId} PUT: {index}, Offset: {offset}, Length: {length}");
            if (!Sessions[sessionId].TryGetValue(index, out var session))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, error, code) = await session.Put(offset, data);

            if (success) return NHACPMessage.OK().ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }

    }

    public static Span<byte> StorageDateTime(byte[]? none) 
        => NHACPStructure.DateTime(DateTime.Now);

    public byte[] StorageClose(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (_, index) = NabuLib.Pop(buffer, 1);
            if (Sessions[sessionId].TryGetValue(index,out var session))
            {
                Log($"{sessionId} CLOSE: {index}");
                session.Close();
            }
            else
            {
                Warning($"{sessionId} CLOSE: {index}: Not Open");
            }

            return Array.Empty<byte>();
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] ErrorDetails(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, code) = NabuLib.Slice(buffer, 1, 2, c => (NHACPError)NabuLib.ToShort(c));
            (i, var maxLength) = NabuLib.Pop(buffer, i);
            Log($"{sessionId} ErrDetail: Code: {code}, MaxLength: {maxLength}");

            var lastCode = code == Sessions[sessionId].LastError ? Sessions[sessionId].LastError : code;
            var lastMessage = code == Sessions[sessionId].LastError ? Sessions[sessionId].LastErrorMessage : ErrorMessages[lastCode];

            return NHACPMessage.Error(lastCode, lastMessage).ToArray();
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public async Task<byte[]> StorageGetBlock(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var block) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, var length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            Log($"{sessionId} BGET: {index}, Block: {block}, Length: {length}, Offset: {block * length}");
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, error, data, code) = await Sessions[sessionId][index].Get(block * length, length);
            if (success) return NHACPMessage.Buffer(data).ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public async Task<byte[]> StoragePutBlock(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var block) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, var length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var data) = NabuLib.Slice(buffer, i, length);
            Log($"{sessionId} BPUT: {index}, Block: {block}, Length: {length}");
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, error, code) = await Sessions[sessionId][index].Put(block * length, data);

            if (success) return NHACPMessage.OK().ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public async Task<byte[]> FileRead(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var flags) = NabuLib.Slice(buffer, i, 2, f => (StorageFlags)NabuLib.ToShort(f));
            (i, var length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            Log($"{sessionId} READ: {index}, Flags: {flags}, Length: {length}");
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, error, data, code) = await Sessions[sessionId][index].Read(length);
            if (success) return NHACPMessage.Buffer(data).ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }
    public async Task<byte[]> FileWrite(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var flags) = NabuLib.Slice(buffer, i, 2, f => (StorageFlags)NabuLib.ToShort(f));
            (i, var length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var data) = NabuLib.Slice(buffer, i, length);
            Log($"{sessionId} WRITE: {index}, Flags: {flags}, Length: {length}");
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, error, code) = await Sessions[sessionId][index].Write(data);
            if (success) return NHACPMessage.OK().ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] FileSeek(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var offset) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, var origin) = NabuLib.Slice(buffer, i, 1, o => (NHACPSeekOrigin)o.Span[0]);
            Log($"{sessionId} SEEK: {index}, Offset: {offset}, Origin: {origin}");
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, pos, error, code) = Sessions[sessionId][index].Seek(offset, origin);
            if (success) return NabuLib.FromInt(pos);

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] FileGetInfo(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }

            var (success, path, error, code) = Sessions[sessionId][index].Info();
            if (success) return NHACPStructure.FileInfo(path).ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] FileSetSize(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var length) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, size, error, code) = Sessions[sessionId][index].SetSize(length);
            if (success) return NHACPMessage.OK().ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] ListDir(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var length) = NabuLib.Pop(buffer, i);
            (i, var pattern) = NabuLib.Slice(buffer, i, length, NHACPStructure.String);
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, error, code) = Sessions[sessionId][index].ListDir(pattern);
            if (success) return NHACPMessage.OK().ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] GetDirEntry(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = NabuLib.Pop(buffer, 1);
            (i, var length) = NabuLib.Pop(buffer, i);
            if (!Sessions[sessionId].ContainsKey(index))
            {
                return ErrorResult(sessionId, NHACPError.BadDescriptor, ErrorMessages[NHACPError.BadDescriptor]);
            }
            var (success, entry, error, code) = Sessions[sessionId][index].GetDirEntry(length);
            if (success)
            {
                if (entry is null) return NHACPMessage.OK().ToArray();
                return NHACPStructure.FileInfo(entry!).ToArray();
            }

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] Remove(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, flags) = NabuLib.Slice(buffer, 1, 2, f => (RemoveFlags)NabuLib.ToShort(f));
            (i, var length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var url) = NabuLib.Slice(buffer, i, length, NHACPStructure.String);

            var removeFile = flags.HasFlag(RemoveFlags.RemoveFile);
            var removeDir = flags.HasFlag(RemoveFlags.RemoveDir);
            var isFile = File.Exists(url);
            var isDir = Directory.Exists(url);

            if (removeFile)
            {
                if (isFile) File.Delete(url);
                else if (isDir) return ErrorResult(sessionId, NHACPError.IsDirectory, "Is Directory");
                else return ErrorResult(sessionId, NHACPError.NotFound, "Not Found"); ;
            }
            else if (removeDir)
            {
                if (isDir) Directory.Delete(url);
                else if (isFile) return ErrorResult(sessionId, NHACPError.InvalidRequest, "Is not a Directory");
                else return ErrorResult(sessionId, NHACPError.NotFound, "Not Found");
            }

            return NHACPMessage.OK().ToArray();
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }


    }

    public byte[] Rename(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, length) = NabuLib.Slice(buffer, 1, 2, NabuLib.ToShort);
            (i, var oldUrl) = NabuLib.Slice(buffer, i, length, NHACPStructure.String);
            (i, length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var newUrl) = NabuLib.Slice(buffer, i, length, NHACPStructure.String);
            
            oldUrl = NabuLib.FilePath(Settings, oldUrl);
            newUrl = NabuLib.FilePath(Settings, newUrl);

            var isFile = File.Exists(oldUrl);
            var isDir = Directory.Exists(oldUrl);
            try
            {
                if (isFile || isDir)
                {
                    if (isFile) File.Move(oldUrl, newUrl);
                    if (isDir) Directory.Move(newUrl, oldUrl);
                    return NHACPMessage.OK().ToArray();
                }
                return ErrorResult(sessionId, NHACPError.NotFound, "Not Found");
            }
            catch (Exception ex)
            {
                return ErrorResult(sessionId, ex);
            }
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] MakeDir(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, length) = NabuLib.Slice(buffer, 0, 2, NabuLib.ToShort);
            (i, var uri) = NabuLib.Slice(buffer, i, length, NHACPStructure.String);
            uri = NabuLib.FilePath(Settings, uri);
            try
            {
                Directory.CreateDirectory(uri);
                return NHACPMessage.OK().ToArray();
            }
            catch (Exception ex)
            {
                return ErrorResult(sessionId, ex);
            }
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPError.InvalidRequest, ErrorMessages[NHACPError.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public byte[] Goodbye(byte sessionId)
    {
        if (Sessions.TryGetValue(sessionId, out NHACPV01Session? value))
        {
            value?.Dispose();
            Sessions.Remove(sessionId);
        }
        return Array.Empty<byte>();
    }

    public override byte Version { get; } = 0x01;
    public override byte[] Commands => new byte[] { 0x8F };

    NHACPOptions Options = NHACPOptions.None;
    short CurrentVersion = 0;
    

    public bool SendCRC { get; private set; }

    protected override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        
        var sessionId = Recv();
        var (length, buffer) = ReadFrame();
        var (i, command) = NabuLib.Pop(buffer, 0);
        byte[] result = Array.Empty<byte>();
        Debug($"Session: {sessionId}, Command: {command}");
        result = command switch
        {
            0x00 => Hello(sessionId, buffer),
            0x01 => await StorageOpen(sessionId, buffer),
            0x02 => await StorageGet(sessionId, buffer),
            0x03 => await StoragePut(sessionId, buffer),
            0x04 => StorageDateTime(buffer).ToArray(),
            0x05 => StorageClose(sessionId, buffer),
            0x06 => ErrorDetails(sessionId, buffer),
            0x07 => await StorageGetBlock(sessionId, buffer),
            0x08 => await StoragePutBlock(sessionId, buffer),
            0x09 => await FileRead(sessionId, buffer),
            0x0a => await FileWrite(sessionId, buffer),
            0x0b => FileSeek(sessionId, buffer),
            0x0c => FileGetInfo(sessionId, buffer),
            0x0d => FileSetSize(sessionId, buffer),
            0x0e => ListDir(sessionId, buffer),
            0x0f => GetDirEntry(sessionId, buffer),
            0x10 => Remove(sessionId, buffer),
            0x11 => Rename(sessionId, buffer),
            0x12 => MakeDir(sessionId, buffer),
            0xEF => Goodbye(sessionId),
            _ => Array.Empty<byte>()
        };

        if (result is null || result.Length == 0) return;

        result = SendCRC ? NabuLib.Concat<byte>(result, new[] { CRC.GenerateCRC8(result) }).ToArray() 
                         : result;
        SendFramed(result!);
    }

    public override void Reset()
    {
        foreach (var session in Sessions.Values)
        {
            session.Dispose();
        }
        Sessions.Clear();
    }

}


