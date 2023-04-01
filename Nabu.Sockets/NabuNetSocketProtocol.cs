using Nabu.Adaptor;
using Nabu.Services;
using System.Net.Sockets;


namespace Nabu.Network;

public class NabuNetSocketProtocol : Protocol
{
    Dictionary<byte, Socket> Sockets { get; } = new();
    public NabuNetSocketProtocol(IConsole<NabuNetSocketProtocol> logger) : base(logger)
    {

    }

    public override byte[] Commands { get; } = new byte[] { 0xA1 };
    public override byte Version => 0x01;

    byte NextIndex()
    {
        for (int i = 0x00; i < 0xFF; i++)
        {
            if (Sockets.ContainsKey((byte)i)) continue;
            return (byte)i;
        }
        return 0xFF;
    }

    #region Frames / Messages
    void Started()
    {
        SendFramed(
            0x80,
            NabuLib.FromShort(Version),
            NabuLib.ToSizedASCII(Emulator.Id)
        );
    }

    void Error(short code, string message)
    {
        SendFramed(
            0x82,
            NabuLib.FromShort(code),
            NabuLib.ToSizedASCII(message) //<-- [length][characters....]
        );
    }
    void Loaded(short index)
    {
        SendFramed(
            0x83,
            NabuLib.FromShort(index)
        );
    }
    void DataBuffer(byte[] buffer)
    {
        SendFramed(
            0x84,
            NabuLib.FromShort((short)buffer.Length),
            buffer
        );
    }

    #endregion

    #region Helpers

    void OnPacketSliced(int next, long length)
    {
        if (next > 0)
            Warning($"Packet length {length - next} != Frame length {length}");
    }

    #endregion

    #region Operations
    async Task Open(byte[] buffer, CancellationToken cancel)
    {
        Log($"NPC: Open");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, short flags) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, byte uriLength) = NabuLib.Pop(buffer, i);
            (i, string uri) = NabuLib.Slice(buffer, i, uriLength, NabuLib.ToASCII);
            OnPacketSliced(i, buffer.Length);


            if (index is not 0xFF)
            {
                if (Sockets.ContainsKey(index))
                {
                    Error(ErrorCodes.Duplicate, $"Socket {index} already exists");
                    return;
                }
            }
            else
            {
                index = NextIndex();
                if (index is 0xFF)
                    Error(500, "All slots full");
            }

            var uriParts = uri.Split(':', 2, StringSplitOptions.TrimEntries);
            if (uriParts.Length < 2)
                Error(400, "Invalid URI");

            var host = uriParts[0];
            if (!int.TryParse(uriParts[1], out var port))
                Error(400, "Invalid Port");

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(host, port);
            Sockets[index] = socket;

            Log($"NA: Adding socket into slot {index}: {uri}");

            Loaded(index);

        }
        catch (Exception ex)
        {
            Error(500, ex.Message);
        }
    }

    async Task Read(byte[] buffer, CancellationToken cancel)
    {
        Log($"NPC: Get");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, short length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            OnPacketSliced(i, buffer.Length);

            if (Sockets.ContainsKey(index) is false)
                Error(404, "Socket not found");

            var data = new byte[length];
            await Sockets[index].ReceiveAsync(data.AsMemory(), cancel);

            Log($"NA: Read from slot {index}:{length} bytes");
            DataBuffer(data);

        }
        catch (Exception ex)
        {
            Error(500, ex.Message);
        }

    }

    async Task Write(byte[] buffer, CancellationToken cancel)
    {
        Log($"NPC: Put");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, short length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var data) = NabuLib.Slice(buffer, i, length);
            OnPacketSliced(i, buffer.Length);

            await Sockets[index].SendAsync(data.AsMemory(), cancel);

            Log($"Writing to slot {index}: OK");
            SendFramed(0x81); // OK

        }
        catch (Exception ex)
        {
            Error(0, ex.Message);
        }

    }

    #endregion


    protected override async Task Handle(byte command, CancellationToken cancel)
    {
        Log($"Start v{Version}");
        Started();
       
        while (cancel.IsCancellationRequested is false)
            try
            {
                var (length, buffer) = ReadFrame();
                if (length is 0)
                    return;

                (int next, command) = NabuLib.Pop(buffer, 0);
                var (_, message) = NabuLib.Slice(buffer, next, length);

                switch (command)
                {

                    case 0x00:
                        Warning($"v{Version} Received: 0, Aborting");
                        break;
                    case 0xEF:
                        break;
                    case 0x01:
                        await Open(message, cancel);
                        break;
                    case 0x02:
                        await Read(message, cancel);
                        break;
                    case 0x03:
                        await Write(message, cancel);
                        break;
                    default:
                        Warning($"Unsupported: {Format(command)}");
                        continue;
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }

        Log($"End v{Version}");
        return;
    }

    public override void Detach()
    {
        foreach (var socket in Sockets.Values)
        {
            socket.Close();
            socket.Dispose();
        }
        base.Detach();
    }

}
