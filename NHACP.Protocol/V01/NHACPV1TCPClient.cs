using Gry.Adapters;
using Microsoft.Extensions.Logging;
using NHACP.V01;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace NHACP.V01;

public record NHACPV1Result(
    bool Success,
    uint Length = 0,
    Memory<byte>? Data = null,
    string? Path = null,
    string? Error = null,
    NHACPErrors ErrorCode = NHACPErrors.Undefined
);

public partial class NHACPV1TCPClient(ILogger logger, AdapterDefinition adaptor) : INHACPStorageHandler
{
    const string NotSupported = "Not Supported";
    const string NotConnected = "Not Connected";
    TcpClient Client { get; } = new()
    {
        NoDelay = true,
        LingerState = new LingerOption(false, 0),
        ReceiveBufferSize = 8,
        SendBufferSize = 8
    };
    ILogger Logger { get; } = logger;
    AdapterDefinition Adaptor { get; } = adaptor;

    [GeneratedRegex(@"^[tT][cC][pP]://(.*):(\d*)$")]
    private static partial Regex Tcp();
    public static bool IsTcp(string uri) => Tcp().Match(uri).Success;

    public async Task<(bool, string, uint, NHACPErrors)> Open(NHACPOpenFlags flags, string uri)
    {
        try
        {
            var uri_parts = Tcp().Match(uri);
            if (uri_parts.Length is 0)
                return (false, "Bad URI", 0, NHACPErrors.InvalidRequest);
            

            var hostname = uri_parts.Captures[0].Value;
            var port = int.Parse(uri_parts.Captures[1].Value);

            await Client.ConnectAsync(hostname, port);

        }
        catch (Exception ex) {
            return (false, ex.Message, 0, NHACPErrors.Undefined);
        }

        return (true, string.Empty, 0, NHACPErrors.Undefined);
    }

    public Task<(bool, string, Memory<byte>, NHACPErrors)> Get(uint offset, uint length, bool realLength = false)
    {
        return Task.FromResult((false, NotSupported, new Memory<byte>([]), NHACPErrors.InvalidRequest));
    }

    public Task<(bool, string, NHACPErrors)> Put(uint offset, Memory<byte> buffer)
    {
        return Task.FromResult((false, NotSupported, NHACPErrors.InvalidRequest));
    }

    public (bool, uint, string, NHACPErrors) Seek(uint offset, NHACPSeekOrigin origin)
    {
        return (false, 0, NotSupported, NHACPErrors.InvalidRequest);
    }

    public (bool, string, string, NHACPErrors) Info()
    {
        return (false, string.Empty, NotSupported, NHACPErrors.InvalidRequest);
    }

    public (bool, uint, string, NHACPErrors) SetSize(uint size)
    {
        return (false, 0, NotSupported, NHACPErrors.InvalidRequest);
    }

    public async Task<(bool, string, Memory<byte>, NHACPErrors)> Read(uint length)
    {
        if (Client.Connected is false) 
            return (false, NotConnected, new([]), NHACPErrors.InvalidRequest);

        var data = new Memory<byte>();
        var readLength = await Client.GetStream().ReadAsync(data);
        return (true, string.Empty, data, NHACPErrors.Undefined);
    }

    public async Task<(bool, string, NHACPErrors)> Write(Memory<byte> buffer)
    {
        if (Client.Connected is false)
            return (false, NotConnected, NHACPErrors.InvalidRequest);

    
        await Client.GetStream().WriteAsync(buffer);
        return (true, string.Empty, NHACPErrors.Undefined);
    }

    public (bool, string, NHACPErrors) ListDir(string pattern)
    {
        return (false, NotSupported, NHACPErrors.InvalidRequest);
    }

    public (bool, string?, string, NHACPErrors) GetDirEntry(byte maxNameLength)
    {
        return (false, null, NotSupported, NHACPErrors.InvalidRequest);
    }

    public uint Position => 0;

    public async Task Close()
    {
        await Task.Run(Client.Close);
    }

    public void End()
    {
        Client.Dispose();
    }
}