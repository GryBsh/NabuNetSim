using Gry;
using Gry.Adapters;
using Gry.Caching;
using Gry.Protocols;
using Gry.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHACP;
using NHACP.Messages;
using NHACP.Messages.V1;
using NHACP.V01;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.RegularExpressions;

namespace NHACP.V01;

[Flags]
public enum NHACPProtocolState : byte
{
    None = 0,
    AdapterAttached = 1,
    SessionMissing = 2
}
public partial class NHACPV01Protocol<TSettings> : Protocol<TSettings>
    where TSettings : AdapterDefinition
{
    private readonly ushort CurrentVersion = 1;
    private readonly IFileCache FileCache;    private readonly IOptions<CacheOptions> cacheOptions;    private readonly IProtocolHostInfo hostInfo;    private readonly ILocationService location;    private readonly HttpClient HttpClient;
    private readonly NHACPOptions Options = NHACPOptions.None;

    public NHACPV01Protocol(
        ILogger<NHACPV01Protocol<TSettings>> logger, 
        HttpClient http, 
        IFileCache fileCache,
        IOptions<CacheOptions> cacheOptions,
        IProtocolHostInfo hostInfo,        ILocationService location
    ) : base(logger)
    {
        Sessions = [];
        HttpClient = http;
        FileCache = fileCache;        this.cacheOptions = cacheOptions;        this.hostInfo = hostInfo;        this.location = location;        
    }

    public override byte[] Messages => [ 0x8F ];
    public bool SendCRC { get; private set; }
    public CacheOptions CacheOptions => cacheOptions.Value;
    public override byte Version { get; } = 0x01;

    void ThrowIf(NHACPProtocolState state, byte? sessionId = null)
    {
        (bool, string) AdapterAttached() => (
            Adapter is not null, 
            "No Adapter Attached"
        );
        
        (bool, string) SessionMissing(byte sessionId) => (
            Sessions.ContainsKey(sessionId) is false, 
            $"No Open Session for: {sessionId}"
        );

        (bool shouldThrow, string errorText) = state switch
        {
            NHACPProtocolState.AdapterAttached 
                => AdapterAttached(),
            NHACPProtocolState.SessionMissing when sessionId is not null 
                => SessionMissing(sessionId.Value),
            _   => (false, string.Empty),
        };

        if (shouldThrow) throw new InvalidOperationException(errorText);
    }

    protected override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        var sessionId = Read();
        var (_, buffer) = ReadFrame();

        var request = new NHACPRequest(sessionId, buffer);            
        
        Debug($"Session: {request.SessionId}, Command: {request.Type}");
        Memory<byte> result = request.Type switch
        {
            0x00 => Hello(request),
            0x01 => await StorageOpen(request),
            0x02 => await StorageGet(request),
            0x03 => await StoragePut(sessionId, buffer),
            0x04 => StorageDateTime(),
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

        if (result.Length == 0) return;

        if (SendCRC)
        {
            var newResult = new Memory<byte>(new byte[result.Length + 1]);
            result.CopyTo(newResult);
            newResult.Span[^1] = CRC.GenerateCRC8(result);
            result = newResult;
        }

        WriteFrame(result);
    }

    private Dictionary<NHACPErrors, string> ErrorMessages { get; } = new()
    {
        [NHACPErrors.Undefined] = "Unknown",
        [NHACPErrors.NotSupported] = "Not Supported",
        [NHACPErrors.NotPermitted] = "Not Permitted",
        [NHACPErrors.NotFound] = "Not Found",
        [NHACPErrors.IOError] = "IO Error",
        [NHACPErrors.BadDescriptor] = "Bad Descriptor",
        [NHACPErrors.OutOfMemory] = "Out of Memory",
        [NHACPErrors.AccessDenied] = "Permission Denied",
        [NHACPErrors.Busy] = "Busy",
        [NHACPErrors.Exists] = "Exists",
        [NHACPErrors.IsDirectory] = "Is Directory",
        [NHACPErrors.InvalidRequest] = "Invalid Request",
        [NHACPErrors.TooManyOpen] = "Too Many Open",
        [NHACPErrors.TooLarge] = "Too Large",
        [NHACPErrors.OutOfSpace] = "Out of Space",
        [NHACPErrors.NoSeek] = "Not Seekable",
        [NHACPErrors.NotADirectory] = "Not a Directory"
    };

    private Dictionary<byte, NHACPV01Session> Sessions { get; }

    public static Memory<byte> StorageDateTime()
        => new NHACPV1DateTime(DateTime.Now); //NHACPMessages.DateTime(DateTime.Now);

    public byte[] ErrorDetails(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, code) = Bytes.Slice(buffer, 1, 2, c => (NHACPErrors)Bytes.ToUShort(c));
            (i, var maxLength) = Bytes.Pop(buffer, i);
            Log($"{sessionId} ErrDetail: Code: {code}, MaxLength: {maxLength}");

            var lastCode = code == Sessions[sessionId].LastError ? Sessions[sessionId].LastError : code;
            var lastMessage = code == Sessions[sessionId].LastError ? Sessions[sessionId].LastErrorMessage : ErrorMessages[lastCode];

            return NHACPMessages.Error(lastCode, lastMessage).ToArray();
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public Memory<byte> FileGetInfo(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = Bytes.Pop(buffer, 1);
            Log($"{sessionId} INFO: {index}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }

            var (success, path, error, code) = value.Info();
            if (success) return NHACPMessages.DirectoryEntry(path, 0);
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var flags) = Bytes.Slice(buffer, i, 2, f => (StorageFlags)Bytes.ToUShort(f));
            (i, var length) = Bytes.Slice(buffer, i, 2, Bytes.ToUShort);
            Log($"{sessionId} READ: {index}, Flags: {flags}, Length: {length}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(
                    sessionId,
                    NHACPErrors.BadDescriptor,
                    ErrorMessages[NHACPErrors.BadDescriptor]
                );
            }
            var (success, error, data, code) = await value.Read(length);
            if (success) return NHACPMessages.Buffer(data).ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(
                sessionId,
                NHACPErrors.InvalidRequest,
                ErrorMessages[NHACPErrors.InvalidRequest]
            );
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
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var offset) = Bytes.Slice(buffer, i, 4, Bytes.ToUInt);
            (i, var origin) = Bytes.Slice(buffer, i, 1, o => (NHACPSeekOrigin)o.Span[0]);
            Log($"{sessionId} SEEK: {index}, Offset: {offset}, Origin: {origin}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }
            var (success, pos, error, code) = value.Seek(offset, origin);
            if (success) return NHACPMessages.Int(pos).ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var length) = Bytes.Slice(buffer, i, 4, Bytes.ToUInt);
            Log($"{sessionId} SETSIZE: {index}, Length: {length}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }
            var (success, size, error, code) = value.SetSize(length);
            if (success) return NHACPMessages.OK().ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var flags) = Bytes.Slice(buffer, i, 2, f => (StorageFlags)Bytes.ToUShort(f));
            (i, var length) = Bytes.Slice(buffer, i, 2, Bytes.ToUShort);
            (i, var data) = Bytes.Slice(buffer, i, length);
            Log($"{sessionId} WRITE: {index}, Flags: {flags}, Length: {length}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(
                    sessionId,
                    NHACPErrors.BadDescriptor,
                    ErrorMessages[NHACPErrors.BadDescriptor]
                );
            }
            var (success, error, code) = await value.Write(data);
            if (success) return NHACPMessages.OK().ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var length) = Bytes.Pop(buffer, i);
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }
            var (success, entry, error, code) = value.GetDirEntry(length);
            if (success)
            {
                if (entry is null) return NHACPMessages.OK().ToArray();
                return NHACPMessages.DirectoryEntry(entry, length).ToArray();
            }

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
        return [];
    }

    public byte[] ListDir(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var length) = Bytes.Pop(buffer, i);
            (i, var pattern) = Bytes.Slice(buffer, i, length, NHACPStructures.String);
            Log($"{sessionId} LIST: {index}, Pattern: {pattern}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }
            var (success, error, code) = value.ListDir(pattern);
            if (success) return NHACPMessages.OK().ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            ThrowIf(NHACPProtocolState.SessionMissing, sessionId);

            var (i, length) = Bytes.Slice(buffer, 0, 2, Bytes.ToUShort);
            (i, var uri) = Bytes.Slice(buffer, i, length, NHACPStructures.String);
            uri = Files.FilePath(Adapter!, uri);
            
            try
            {
                Directory.CreateDirectory(uri);
                return NHACPMessages.OK().ToArray();
            }
            catch (Exception ex)
            {
                return ErrorResult(sessionId, ex);
            }
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, flags) = Bytes.Slice(buffer, 1, 2, f => (RemoveFlags)Bytes.ToUShort(f));
            (i, var length) = Bytes.Pop(buffer, i);
            (i, var url) = Bytes.Slice(buffer, i, length, NHACPStructures.String);

            Log($"{sessionId} REMOVE: {url}, Flags: {flags}");

            url = Files.FilePath(Adapter!, url);
            var removeFile = flags.HasFlag(RemoveFlags.RemoveFile);
            var removeDir = flags.HasFlag(RemoveFlags.RemoveDir);
            var isFile = File.Exists(url);
            var isDir = Directory.Exists(url);

            if (removeFile)
            {
                if (isFile) File.Delete(url);
                else if (isDir) return ErrorResult(sessionId, NHACPErrors.IsDirectory, "Is Directory");
                else return ErrorResult(sessionId, NHACPErrors.NotFound, "Not Found");
            }
            else if (removeDir)
            {
                if (isDir) Directory.Delete(url);
                else if (isFile) return ErrorResult(sessionId, NHACPErrors.InvalidRequest, "Is not a Directory");
                else return ErrorResult(sessionId, NHACPErrors.NotFound, $"Not Found {url}");
            }

            return NHACPMessages.OK().ToArray();
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, length) = Bytes.Slice(buffer, 1, 2, Bytes.ToUShort);
            (i, var oldUrl) = Bytes.Slice(buffer, i, length, NHACPStructures.String);
            (i, length) = Bytes.Slice(buffer, i, 2, Bytes.ToUShort);
            (i, var newUrl) = Bytes.Slice(buffer, i, length, NHACPStructures.String);

            oldUrl = Files.FilePath(Adapter!, oldUrl);
            newUrl = Files.FilePath(Adapter, newUrl);

            var isFile = File.Exists(oldUrl);
            var isDir = Directory.Exists(oldUrl);
            try
            {
                if (isFile || isDir)
                {
                    if (isFile) File.Move(oldUrl, newUrl);
                    if (isDir) Directory.Move(newUrl, oldUrl);
                    return NHACPMessages.OK().ToArray();
                }
                return ErrorResult(sessionId, NHACPErrors.NotFound, "Not Found");
            }
            catch (Exception ex)
            {
                return ErrorResult(sessionId, ex);
            }
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public override void Reset()
    {
        foreach (var session in Sessions.Values)
        {
            session.Dispose();
        }
        Sessions.Clear();
    }

    public byte[] StorageClose(byte sessionId, Memory<byte> buffer)
    {
        try
        {
            var (_, index) = Bytes.Pop(buffer, 1);
            if (Sessions[sessionId].TryGetValue(index, out var session))
            {
                Log($"{sessionId} CLOSE: {index}");
                session.Close();
            }
            else
            {
                Warning($"{sessionId} CLOSE: {index}: Not Open");
            }

            return [];
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var block) = Bytes.Slice(buffer, i, 4, Bytes.ToUInt);
            (i, var length) = Bytes.Slice(buffer, i, 2, Bytes.ToUShort);
            Log($"{sessionId} BGET: {index}, Block: {block}, Length: {length}, Offset: {block * length}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }
            var (success, error, data, code) = await value.Get(block * length, length);
            if (success) return NHACPMessages.Buffer(data).ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var offset) = Bytes.Pop(buffer, i);
            (i, var length) = Bytes.Slice(buffer, i, 2, Bytes.ToUShort);
            (i, var data) = Bytes.Slice(buffer, i, length);

            Log($"{sessionId} PUT: {index}, Offset: {offset}, Length: {length}");
            if (!Sessions[sessionId].TryGetValue(index, out var session))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }
            var (success, error, code) = await session.Put(offset, data);

            if (success) return NHACPMessages.OK().ToArray();

            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
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
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var block) = Bytes.Slice(buffer, i, 4, Bytes.ToUInt);
            (i, var length) = Bytes.Slice(buffer, i, 2, Bytes.ToUShort);
            (i, var data) = Bytes.Slice(buffer, i, length);
            Log($"{sessionId} BPUT: {index}, Block: {block}, Length: {length}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }
            var (success, error, code) = await value.Put(block * length, data);

            if (success) return NHACPMessages.OK().ToArray();
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }



    [GeneratedRegex("ftp://.*")]
    private static partial Regex Ftp();

    [GeneratedRegex("0x\\d*")]
    private static partial Regex Memory();

    private byte[] ErrorResult(byte sessionId, Exception ex, [CallerMemberName] string source = "Unknown", [CallerLineNumber] long line = 0)
    {
        string errorMessage(NHACPErrors error)
            => $"{ErrorMessages[error]} at {source}:{line}";

        var (code, error) = ex switch
        {
            FileNotFoundException => (NHACPErrors.NotFound, errorMessage(NHACPErrors.NotFound)),
            UnauthorizedAccessException => (NHACPErrors.AccessDenied, errorMessage(NHACPErrors.AccessDenied)),
            SecurityException => (NHACPErrors.AccessDenied, errorMessage(NHACPErrors.AccessDenied)),
            IOException => (NHACPErrors.IOError, errorMessage(NHACPErrors.IOError)),
            _ => (NHACPErrors.Undefined, errorMessage(NHACPErrors.Undefined))
        };
        Logger.LogError(ex, error);
        return ErrorResult(sessionId, code, string.IsNullOrWhiteSpace(ex.Message) ? error : ex.Message);
    }

    private byte[] ErrorResult(byte sessionId, NHACPErrors code, string error)
    {
        if (Sessions.TryGetValue(sessionId, out var session))
        {
            session.LastError = code;
            session.LastErrorMessage = error;
        }
        
        Error($"{sessionId} Error: {code}: {error}");
        return NHACPMessages.Error(code, error).ToArray();
    }

    private Memory<byte> Hello(NHACPRequest request)//byte sessionId, Memory<byte> buffer)
    {
        NHACPV1Hello? hello = null;
        var sessionId = request.SessionId;
        try
        {
            hello = new NHACPV1Hello(request);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ex.Message);
        }

        if (Sessions.TryGetValue(sessionId, out var session))
        {
            try { session.Dispose(); } catch { }
        }

        if (sessionId is 0xFF)
        {
            sessionId = NextSessionId();
        }
        
        Sessions[sessionId] = [];

        SendCRC = Options.HasFlag(NHACPOptions.CRC8);
        Log($"Session: {sessionId}, Started");
        //return NHACPMessages.NHACPStarted(sessionId, CurrentVersion, "NHACPy").ToArray();
        var r = new NHACPV1SessionStarted(
            sessionId, 
            CurrentVersion, 
            $"{hostInfo.Name} v{hostInfo.Version}"
        );

        return new([
            r.Type,
            ..r.Body.Span
        ]);
    }

    public async Task<Memory<byte>> StorageOpen(NHACPRequest request)
    {
        var sessionId = request.SessionId;
        try
        {
            /*
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var flags) = Bytes.Slice(buffer, i, 2, o => (NHACPOpenFlags)Bytes.ToUShort(o));
            (i, var length) = Bytes.Pop(buffer, i);
            (i, var uri) = Bytes.Slice(buffer, i, length, NHACPStructures.String);
            if (index is 0xFF) index = NextDescriptorForSession(sessionId);
            */

            var open = new NHACPV1StorageOpen(request);
            
            var index = open.Descriptor;
            if (index == 0xFF) index = NextDescriptorForSession(sessionId);
            var flags = open.Flags;
            var uri = open.Url!;

            Log($"{sessionId} Open: {index}, Flags: {flags}, Uri: {uri}");

            var (isSymLink, path) = uri switch
            {
                _ when Net.IsHttp(uri) => (false, Files.Uri(Adapter!, uri)),
                _ when Memory().IsMatch(uri) => (false, Files.Uri(Adapter!, uri)),
                _ when NHACPV1TCPClient.IsTcp(uri) => (false, Files.Uri(Adapter!, uri)),

                _ => Files.PathInfo(Adapter!, uri)
            };

            Sessions[sessionId][index] = path switch
            {
                _ when Net.IsHttp(path)
                            => new NHACPV1HttpHandler(                                Logger,                                 Adapter!,                                 HttpClient,                                 FileCache,                                 cacheOptions,                                 location                            ),
                _ when path.StartsWith("0x")
                            => new NHACPV1RamHandler(Logger, Adapter!),
                _ when NHACPV1TCPClient.IsTcp(path)
                            => new NHACPV1TCPClient(Logger, Adapter!),
                _ => new NHACPV1LocalHandler(Logger, Adapter!, isSymLink, uri)
            };

            var (success, error, size, code) = await Sessions[sessionId][index].Open(flags, path);

            if (success) //return NHACPMessages.StorageLoaded(index, size).ToArray();
                return new NHACPV1StorageLoaded(index, size);
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(sessionId, NHACPErrors.InvalidRequest, ErrorMessages[NHACPErrors.InvalidRequest]);
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    public async Task<Memory<byte>> StorageGet(NHACPRequest request)
    {
        var sessionId = request.SessionId;
        try
        {
            /*
            var (i, index) = Bytes.Pop(buffer, 1);
            (i, var offset) = Bytes.Pop(buffer, i);
            (i, var length) = Bytes.Slice(buffer, i, 2, Bytes.ToUShort);
            */

            var get = new NHACPV1StorageGet(request);
            
            var index = get.Descriptor;
            var offset = get.Offset;
            var length = get.Length;


            Log($"{sessionId} GET: {index}, Offset: {offset}, Length: {length}");
            if (!Sessions[sessionId].TryGetValue(index, out INHACPStorageHandler? value))
            {
                return ErrorResult(sessionId, NHACPErrors.BadDescriptor, ErrorMessages[NHACPErrors.BadDescriptor]);
            }

            if (length > NHACPConstants.MaxDataSize)
            {
                return ErrorResult(
                    sessionId, 
                    NHACPErrors.InvalidRequest, 
                    ErrorMessages[NHACPErrors.InvalidRequest]
                );
            }

            var (success, error, data, code) = await value.Get(offset, length, true);

            if (success) return new NHACPV1DataBuffer(index, data);
            return ErrorResult(sessionId, code, error);
        }
        catch (IndexOutOfRangeException)
        {
            return ErrorResult(
                sessionId, 
                NHACPErrors.InvalidRequest, 
                ErrorMessages[NHACPErrors.InvalidRequest]
            );
        }
        catch (Exception ex)
        {
            return ErrorResult(sessionId, ex);
        }
    }

    private byte NextSessionId()
    {
        for (int i = 0x01; i < 0xFF; i++)
        {
            if (!Sessions.TryGetValue((byte)i, out _)) return (byte)i;
        }
        return 0xFF;
    }

    private byte NextDescriptorForSession(byte sessionId)
    {
        for (int i = 0x00; i < 0xFF; i++)
        {
            if (!Sessions[sessionId].TryGetValue((byte)i, out _)) return (byte)i;
        }
        return 0xFF;
    }
}