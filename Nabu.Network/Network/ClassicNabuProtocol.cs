using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Messages;
using Nabu.Services;

namespace Nabu.Network;

public class ClassicNabuProtocol : Protocol
{
    NabuNetwork Network { get; }
    public override byte[] Commands { get; } = new byte[] { 0x83 };
    public override byte Version => 0x84;
    public short Channel { get; set; }
    public bool ChannelKnown => Channel is > 0 and < 0x100;

    AdaptorSettings Settings { get; set; } = new NullAdaptorSettings();

    public ClassicNabuProtocol(
        IConsole<ClassicNabuProtocol> logger,
        NabuNetwork network) : base(logger)
    {
        Network = network;
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
    ///  Handles the GetStatus Message - 0x82
    /// </summary>
    void GetStatus()
    {
        short data = NabuLib.ToShort(Recv(1));

        switch (data)
        {
            case StatusMessage.Signal: // <- NA Channel Lock Status
                if (ChannelKnown)
                {
                    Debug($"NPC: {nameof(StatusMessage.Signal)}? NA: {nameof(Status.SignalLock)}");
                    StatusResult(Status.SignalLock);
                }
                else
                {
                    Debug($"NPC: {nameof(StatusMessage.Signal)}? NA: {nameof(Status.NoSignal)}");
                    StatusResult(Status.NoSignal);
                }
                break;
            case StatusMessage.AdaptorReady: // <-- NPC Program/DOS Loaded
                Log($"NPC: Finished? NA: {nameof(Message.Finished)}");
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
        short data = NabuLib.ToShort(Recv(2));
        if (data is > 0 and < 0x100)
        {
            Channel = data;
        }
        else
        {
            Channel = 0;
            return;
        }
        Confirmed();
        Log($"Channel Code: {Channel:X04}");
    }

    /// <summary>
    ///  Handles the Segment Request message - 0x84
    /// </summary>  
    /// <returns>A Task to await fetching the program image from disk/http</returns>
    async Task SegmentRequest()
    {
        short segment = Recv();
        int pak = NabuLib.ToInt(Recv(3));

        // Anything packet except the time packet...
        if (pak is 1 && started is null) 
            started = DateTime.Now;
        // RACERS START YOUR ENGINES!

        Log($"NPC: Segment: {segment:x04}, PAK: {FormatTriple(pak)}, NA: {nameof(StateMessage.Confirmed)}");
        Confirmed();
        if (segment == 0x00 &&
            pak == Message.TimePak)
        {
            Log("NPC: What Time it is?");
            SendPacket(pak,TimePacket(), last: true);
            return;
        }

        // Anything packet except the time packet...
        if (pak is 1 && segment is 0 && started is null) 
            started = DateTime.Now;
        // RACERS START YOUR ENGINES!

        // Network Emulator
        var (type, segmentData) = await Network.Request(Settings, pak);

        if (type is ImageType.None)
        {
            Error("No Image Found or Error");
            Unauthorized();
            return;
        }

        var (last, payload) = type switch
        {
            ImageType.Raw => NabuLib.SliceFromRaw(Logger, segment, pak, segmentData),
            ImageType.Pak => NabuLib.SliceFromPak(Logger, segment, segmentData),
            ImageType.EncryptedPak => NabuLib.SliceFromPak(Logger, segment, NabuLib.Unpak(segmentData))
        };

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
        var (_, payload) = NabuLib.SliceFromRaw(Logger, 0, Message.TimePak, buffer);
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

    DateTime? started = null;

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

        Debug($"NA: {nameof(ServiceStatus.Authorized)}, NPC: {nameof(Message.ACK)}");

        Authorized();

        buffer = EscapeBytes(buffer).ToArray();
        Debug($"NA: Sending Packet, {buffer.Length} bytes");

        //var start = DateTime.Now;
        Send(buffer);
        //var stop = DateTime.Now;

        Finished();        //Epilog
        if (last)
        {
            var finished = DateTime.Now;
            Network.UncachePak(base.Settings, pak);
            GC.Collect();
            
            if (started is null) return; // Time Packet is not timed.

            var elapsed = finished - started.Value;
            Log($"Finished in {elapsed.TotalSeconds:0.000}s");
            started = null;
        }
    }
    #endregion
    
    public override bool Attach(AdaptorSettings settings, Stream stream)
    {
        var success = base.Attach(settings, stream);
        if (success is false) return false;

        Channel = settings.AdapterChannel;
        Settings = settings;
        return true;
    }

    public override async Task Handle(byte incoming, CancellationToken cancel)
    {
        switch (incoming)
        {
            #region NabuNet "Classic" Messages
            case 0:
                Warning($"NA: Aborting");
                break;
            case Message.Reset:
                Ack();
                Log($"NPC: {nameof(Message.Reset)}, NA: {nameof(Message.ACK)} {nameof(StateMessage.Confirmed)}");
                Confirmed();
                break;
            case Message.SetStatus:
                Ack();
                Log($"NPC: {nameof(Message.SetStatus)}, NA: {nameof(Message.ACK)}");
                byte[] status = Recv(2);
                switch (status[0])
                {
                    case 0x0F:
                        Log("NPC: HCCA Idle");
                        break;
                    case 0x1F:
                        Log("NPC: HCCA Transfer");
                        break;
                    default:
                        Log($"NPC: Status: {Format(status[0])}");
                        break;
                }
                Log($"NA: {nameof(StateMessage.Confirmed)}");
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
                Debug($"NPC: {nameof(Message.PacketRequest)}, NA: {nameof(Message.ACK)}");
                await SegmentRequest();
                break;
            case Message.ChangeChannel:
                Ack();
                Log($"NPC: {nameof(Message.ChangeChannel)}, NA: {nameof(Message.ACK)}");
                ChannelChange();
                break;
            #endregion
            default:
                Warning($"Unsupported: {Format(incoming)}");
                break;
        }

        return;
    }
}
