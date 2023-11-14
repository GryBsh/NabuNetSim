using Nabu.Messages;
using Nabu.Services;
using System.Reactive.Linq;
using YamlDotNet.Serialization;

namespace Nabu.Network;

public class ClassicNabuProtocol : Protocol
{
    private DateTime? Started = null;

    public ClassicNabuProtocol(
        ILog<ClassicNabuProtocol> logger,
        INabuNetwork network
    ) : base(logger)
    {
        Network = network;
        Adaptor = new NullAdaptorSettings();
    }

    public ushort Channel { get; set; }
    public bool ChannelKnown => Channel is > 0 and < 0x100;
    public override byte[] Commands { get; } = new byte[] { 0x83 };
    public override byte Version => 0x84;
    private INabuNetwork Network { get; }

    //AdaptorSettings Settings { get; set; } = new NullAdaptorSettings();

    #region NabuNet Responses

    private void Ack() => Write(Message.ACK);

    private void Authorized()
    {
        Write(ServiceStatus.Authorized);               //Prolog
        Read(Message.ACK);
    }

    private void Confirmed() => Write(StateMessage.Confirmed);

    private void Finished() => Write(Message.Finished);

    private void StatusResult(byte value)
    {
        Write(value);
        Finished();
    }

    private void Unauthorized()
    {
        Write(ServiceStatus.Unauthorized);
        Read(Message.ACK);
    }

    //Epilog

    #endregion NabuNet Responses

    #region NabuNet Operations

    /// <summary>
    ///  Handles the Channel Change Message - 0x85
    /// </summary>
    private void ChannelChange()
    {
        ushort data = NabuLib.ToUShort(Read(2));
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
    ///  Handles the GetStatus Message - 0x82
    /// </summary>
    private void GetStatus()
    {
        ushort data = NabuLib.ToUShort(Read(1));

        switch (data)
        {
            case StatusMessage.Signal: // <- NA Channel Lock Status
                if (ChannelKnown)
                {
                    Debug($"NPC: {nameof(StatusMessage.Signal)}? NA: {nameof(Status.Good)}");
                    StatusResult(Status.Good);
                }
                else
                {
                    Debug($"NPC: {nameof(StatusMessage.Signal)}? NA: {nameof(Status.NoSignal)}");
                    StatusResult(Status.NoSignal);
                }
                break;

            case StatusMessage.Subscription: // <-- NPC Program/DOS Loaded
                Log($"NPC: Subscription?");
                if (Network.Source(Adaptor)?.EnableExploitLoader is true)
                {
                    Log($"NA: No Subscription");
                    StatusResult(Status.None);
                }
                else
                {
                    Log($"NA: Subscribed");
                    StatusResult(Status.Good);
                }
                break;

            default:
                Log($"Unsupported Status: {data:X02}");
                break;
        }
    }

    /// <summary>
    ///  Handles the Segment Request message - 0x84
    /// </summary>
    /// <returns>A Task to await fetching the program image from disk/http</returns>
    private async Task SegmentRequest()
    {
        short segment = Read();
        int pak = NabuLib.ToInt(Read(3));

        // Anything packet except the time packet...
        if (pak is not Message.TimePak && Started is null)
        {
            Started = DateTime.Now;
            //NabuLib.StartSafeNoGC(65536);
        }
        // RACERS START YOUR ENGINES!
        //if (pak is 0x191) pak = 0x15C;
        Log($"NPC: Segment: {segment:x04}, PAK: {FormatTriple(pak)}, NA: {nameof(StateMessage.Confirmed)}");
        Confirmed();
        if (segment == 0x00 &&
            pak == Message.TimePak)
        {
            var time = TimePacket();
            Log("NPC: What Time it is?");
            SendPacket(pak, time, time.Length, last: true);
            return;
        }

        // Network Emulator
        var (type, segmentData) = await Network.Request(Adaptor, pak);

        // Anything packet except the time packet...
        if (pak is 1 && segment is 0 && Started is null)
            Started = DateTime.Now;
        // RACERS START YOUR ENGINES!

        if (type is ImageType.None)
        {
            Error("No Image Found or Error");
            Unauthorized();
            return;
        }

        var (last, payload) = type switch
        {
            ImageType.Pak => NabuLib.SliceFromPak(Logger, segment, segmentData),
            ImageType.EncryptedPak => NabuLib.SliceFromPak(Logger, segment, NabuLib.Unpak(segmentData)),
            _ => NabuLib.SliceFromRaw(Logger, segment, pak, segmentData)
        };

        if (payload.Length == 0) Unauthorized();
        else SendPacket(pak, payload, segmentData.Length, last: last);
    }

    private void SetStatus()
    {
        byte[] status = Read(2);
        switch (status[0])
        {
            case Status.None:
                Log("NPC: Status: Idle");
                break;

            case Status.Good:
                Log("NPC: Status: All Good");
                break;

            default:
                Log($"NPC: Status: {Format(status[0])}");
                break;
        }
        Log($"NA: {nameof(StateMessage.Confirmed)}");
        Confirmed();
    }

    #endregion NabuNet Operations

    #region NabuNet Packets

    /// <summary>
    ///     Escapes the Escapes with Escape.
    /// </summary>
    private static IEnumerable<byte> EscapeBytes(Memory<byte> sequence)
    {
        foreach (byte b in sequence.ToArray())
        {
            if (b is Message.Escape)
                yield return Message.Escape;
            
            yield return b;
        }
    }

    /// <summary>
    ///     Sends a packet to the device
    /// </summary>
    private void SendPacket(int pak, Memory<byte> buffer, int totalLength, bool last = false)
    {
        if (buffer.Length > Constants.MaxPacketSize)
        {
            Error("Packet too large");
            Unauthorized();
            return;
        }

        Debug($"NA: {nameof(ServiceStatus.Authorized)}, NPC: {nameof(Message.ACK)}");

        Authorized();

        var output = EscapeBytes(buffer);
        Debug($"NA: Sending Packet, {buffer.Length} bytes");

        Write(output.ToArray());
        Finished();        //Epilog

        if (last)
        {
            var finished = DateTime.Now;

            Network.UnCachePak(base.Adaptor, pak);

            if (Started is null) return; // Time Packet is not timed.

            //NabuLib.EndSafeNoGC();
            TransferRate(Started.Value, finished, totalLength);
            Started = null;
            var source = Network.Source(Adaptor);
            if (Adaptor.ReturnToProgram is not null &&
                (pak is not 1 || source?.EnableExploitLoader is false)
            ){
                Adaptor.Source = Adaptor.ReturnToSource!;
                Adaptor.Program = Adaptor.ReturnToProgram;
                Adaptor.ReturnToSource = null;
                Adaptor.ReturnToProgram = null;
            }
        }
    }

    /// <Summary>
    ///     Send the time packet to the device - segment: 00 pak: 7FFFFF
    /// </summary>
    private Memory<byte> TimePacket()
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

    #endregion NabuNet Packets

    public override bool Attach(AdaptorSettings settings, Stream stream)
    {
        var success = base.Attach(settings, stream);
        if (success is false) return false;

        Channel = settings.AdapterChannel;
        Adaptor = settings;
        return true;
    }

    protected override async Task Handle(byte incoming, CancellationToken cancel)
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
                SetStatus();
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

            #endregion NabuNet "Classic" Messages

            default:
                Warning($"Unsupported: 0x{Format(incoming)}");
                break;
        }

        return;
    }
}