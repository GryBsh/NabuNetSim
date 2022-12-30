using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Messages;

namespace Nabu.Network;

public class NabuNetProtocol : Protocol
{
    NetworkEmulator Network { get; }
    NabuNetAdaptorState State;
    public override byte Identifier => 0x83;
    public override string Name => "NabuNet";
    protected override byte Version => 0x01;

    public NabuNetProtocol(
        ILogger<NabuNetProtocol> logger,
        NetworkEmulator network) : base(logger)
    {
        Network = network;
        State = new();
    }

    #region NabuNet Responses

    void Authorized()
    {
        Send(ServiceStatus.Authorized);               //Prolog
        Recv(Message.ACK);
    }
    void Unauthorized()
    {
        Send(ServiceStatus.Unauthorized);
        Recv(Message.ACK);
    }

    void Ack() => Send(Message.ACK);
    void Confirmed() => Send(StateMessage.Confirmed);
    void Finished() => Send(Message.Finished);        //Epilog

    void StatusResult(byte value)
    {
        Send(value);
        Finished();
    }

    #endregion

    #region NabuNet Operations

    /// <summary>
    ///  Handles the GetStatis Message - 0x82
    /// </summary>
    void GetStatus()
    {
        short data = NABU.ToShort(Recv(1));

        switch (data)
        {
            case StatusMessage.Signal: // <- NA Channel Lock Status
                if (State.ChannelKnown)
                {
                    Log($"NPC: {nameof(StatusMessage.Signal)}? NA: {nameof(Status.SignalLock)}");
                    StatusResult(Status.SignalLock);
                }
                else
                {
                    Log($"NPC: {nameof(StatusMessage.Signal)}? NA: {nameof(Status.NoSignal)}");
                    StatusResult(Status.NoSignal);
                }
                break;
            case StatusMessage.MysteryStatus: // <-- NPC Program/DOS Loaded
                Log($"NPC: 1E? NA: {nameof(Message.Finished)}?");
                Finished();
                break;
            default:
                Log($"Unsupported Status: {data:X02}");
                break;
        }
    }

    /// <summary>
    ///  Handles the Channel Change Message - 0x85
    /// </summary>
    void ChannelChange()
    {
        short data = NABU.ToShort(Recv(2));
        if (data is > 0 and < 0x100)
        {
            State.Channel = data;
        }
        else
        {
            State.Channel = 0;
            return;
        }
        Confirmed();
        Log($"Channel Code: {State.Channel:X04}");
    }

    /// <summary>
    ///  Handles the Segment Request message - 0x84
    /// </summary>
    /// <returns>A Task to await fetching the program image from disk/http</returns>
    async Task SegmentRequest()
    {
        short segment = Recv();
        int pak = NABU.ToInt(Recv(3));

        Log($"NPC: Segment: {segment:x04}, PAK: {FormatTriple(pak)}, NA: {nameof(StateMessage.Confirmed)}");
        Confirmed();
        if (segment == 0x00 &&
            pak == Message.TimePak
        )
        {
            Log("NPC: What Time it is?");
            SendPacket(TimePacket(), last: true);
            return;
        }

        // Network Emulator
        var (type, segmentData) = await Network.Request(pak);

        if (type is ImageType.None)
        {
            Error("No Image Found or Error");
            Unauthorized();
            return;
        }

        bool last = false;
        byte[] payload = Array.Empty<byte>();

        if (type == ImageType.Raw)
            (last, payload) = NABU.SliceRaw(Logger, segment, pak, segmentData);
        else
            (last, payload) = NABU.SlicePak(Logger, segment, segmentData);

        if (payload.Length == 0) Unauthorized();
        else SendPacket(pak, payload, last: last);
    }

    /// <Summary>
    ///     Send the time packet to the device - segment: 00 pak: 7FFFFF
    /// </summary>
    byte[] TimePacket()
    {
        //byte[] buffer = { 0x02, 0x02, 0x02, 0x54, 0x01, 0x01, 0x00, 0x00, 0x00 };
        var now = DateTime.Now;
        byte[] buffer = {
            0x02,
            0x02,
            (byte)(now.DayOfWeek + 1),      //Day Of Week
            (byte)(now.Year - 1900),        //Year
            (byte)now.Month,                //Month 
            (byte)now.Day,                  //Day
            (byte)now.Hour,                 //Hour
            (byte)now.Minute,               //Minute
            (byte)now.Second                //Second
        };
        var (_, payload) = NABU.SliceRaw(Logger, 0, Message.TimePak, buffer);
        return payload;
    }

    #endregion

    #region NabuNet Packets

    /// <summary>
    ///     Escapes the Escape bytes in the packet with Escape.
    /// </summary>
    static IEnumerable<byte> EscapeBytes(IEnumerable<byte> sequence)
    {
        foreach (byte b in sequence)
        {
            if (b == Message.Escape)
            {
                yield return Message.Escape;
                yield return b;
            }
            else
                yield return b;
        }
    }

    /// <summary>
    ///     Sends a packet to the device
    /// </summary>
    void SendPacket(int pak, byte[] buffer, bool last = false)
    {
        if (buffer.Length > Constants.MaxPacketSize)
        {
            Error("Packet too large");
            Unauthorized();
            return;
        }

        Log($"NA: {nameof(ServiceStatus.Authorized)}, NPC: {nameof(Message.ACK)}");

        Authorized();

        buffer = EscapeBytes(buffer).ToArray();
        Log($"NA: Sending Packet, {buffer.Length} bytes");

        var start = DateTime.Now;
        Send(buffer);
        var stop = DateTime.Now;

        Finished();                      //Epilog

        Task.Run(() => TransferRatePrinter(start, stop, buffer.Length));
        if (last)
        {
            Network.UncachePak(pak);
            GC.Collect();
        }
    }

    void TransferRatePrinter(DateTime start, DateTime stop, int length)
    {
        var elapsed = stop - start;
        var rate = length / elapsed.TotalSeconds / 1024;
        var unit = "Kbps";
        if (rate > 1024)
        {
            rate = rate / 1024;
            unit = "Mbps";
        }
        Log($"NPC: Transfer Rate: {rate:0.00} {unit}");
    }

    #endregion

    #region RetroNet
    async Task RequestStoreHttpGet()
    {
        byte index = Recv();
        string url = Reader.ReadString();
        Log($"RequestStore HTTP Get {index}: {url}");
        var (success, _) = await Network.StorageOpen(index, url);
        Send(NABU.FromBool(success));
    }

    void RequestStoreGetSize()
    {
        short index = Recv();

        var size = Network.GetResponseSize(index);
        Log($"RequestStore Get Size {index}: Size: {size}");
        var sizes = NABU.FromShort((short)size);
        Send(sizes);
    }

    void RequestStoreGetData()
    {
        short index = Recv();
        short offset = NABU.ToShort(Recv(2));
        short length = NABU.ToShort(Recv(2));
        Log($"RequestStore.Get Response {index}: Offset: {offset}, Length: {length} ");
        var data = Network.StorageGet(index, offset, length);
        SlowerSend(data);
    }

    void Telnet()
    {
        Warning("A6 : Telnet not supported");
        Send(0x00); // This should terminate receiving bytes until null (a null terminated string) 
                    // on the NABU and any software looking for a 0x00 byte from a disconnect.
        return;
    }
    #endregion

    public override bool Attach(AdaptorSettings settings, Stream stream)
    {
        var success = base.Attach(settings, stream);
        if (success is false) return false;

        State = new()
        {
            Channel = settings.AdapterChannel
        };
        Network.SetState(settings);

        return true;
    }

    public override async Task<bool> Listen(byte incoming)
    {
        switch (incoming)
        {
            #region NabuNet "Classic" Messages
            case 0:
                Log($"NA: Received 0, Disconnected");
                return false;
            case Message.Reset:
                Ack();
                Log($"NPC: {nameof(Message.Reset)}, NA: {nameof(Message.ACK)} {nameof(StateMessage.Confirmed)}");
                Confirmed();
                break;
            case Message.MagicalMysteryMessage:
                Ack();
                Log($"NPC: {nameof(Message.MagicalMysteryMessage)}: {FormatSeperated(Recv(2))}, NA: {nameof(StateMessage.Confirmed)}");
                Confirmed();
                break;
            case Message.GetStatus:
                Ack();
                Log($"NPC: {nameof(Message.GetStatus)}, NA: {nameof(Message.ACK)}");
                GetStatus();
                break;
            case Message.StartUp:
                Ack();
                Log($"NPC: {nameof(Message.StartUp)}, NA: {nameof(Message.ACK)}");
                Confirmed();
                Log($"NA: {nameof(StateMessage.Confirmed)}");
                break;
            case Message.PacketRequest:
                Ack();
                Log($"NPC: {nameof(Message.PacketRequest)}, NA: {nameof(Message.ACK)}");
                await SegmentRequest();
                break;
            case Message.ChangeChannel:
                Ack();
                Log($"NPC: {nameof(Message.ChangeChannel)}, NA: {nameof(Message.ACK)}");
                ChannelChange();
                break;
            #endregion

            #region RetroNET Messages

            case RetroNetMessage.RequestStoreHttpGet:
                await RequestStoreHttpGet();
                break;
            case RetroNetMessage.RequestStoreGetSize:
                RequestStoreGetSize();
                break;
            case RetroNetMessage.RequestStoreGetData:
                RequestStoreGetData();
                break;

            #endregion

            default:
                break;
        }

        return false;
    }

    public override void Listening() { }
}
