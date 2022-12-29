using Nabu.Messages;
using Nabu.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Adaptor;

public partial class AdaptorEmulator
{
    #region Status / Channel Change

    void GetStatus()
    {
        short data = NABU.ToShort(Recv(1));

        switch (data)
        {
            case StatusMessage.Signal: // <- NA Channel Lock Status
                if (State.ChannelKnown)
                {
                    Log($"NPC: {nameof(StatusMessage.Signal)}? NA: {nameof(Status.SignalLock)}");
                    Send(Status.SignalLock);
                    Send(Message.Finished);
                }
                else
                {
                    Log($"NPC: {nameof(StatusMessage.Signal)}? NA: {nameof(Status.NoSignal)}");
                    Send(Status.NoSignal);
                    Send(Message.Finished);
                }
                break;
            case 0x1E: // <-- NPC Program/DOS Loaded
                Log($"NPC: 1E? NA: {nameof(Message.Finished)}");
                Send(Message.Finished);
                break;
            default:
                Log($"Unsupported Status: {data:X02}");
                break;
        }

    }

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
        Send(StateMessage.Confirmed);
        Log($"Channel Code: {State.Channel:X04}");
    }

    #endregion

    #region Segment Request
    async Task SegmentRequest()
    {
        short segment = Recv();
        int pak = NABU.ToInt(Recv(3));

        Log($"NPC: Segment: {segment:x04}, PAK: {FormatTriple(pak)}, NA: {nameof(StateMessage.Confirmed)}");
        Send(StateMessage.Confirmed);
        if (segment == 0x00 &&
            pak == Message.TimePak
        )
        {
            Log("NPC: What Time it is?");
            SendPacket(TimePacket());
            return;
        }

        // Network Emulator
        var (type, segmentData) = await Network.Request(pak);

        if (type is ImageType.None)
        {
            Error("No Image Found or Error");
            Send(ServiceStatus.Unauthorized);
            return;
        }

        byte[] payload = Array.Empty<byte>();

        if (type == ImageType.Nabu)
            payload = NABU.SliceRaw(Logger, segment, pak, segmentData);
        else
            payload = NABU.SlicePak(Logger, segment, segmentData);

        if (payload.Length == 0) Send(ServiceStatus.Unauthorized);
        else SendPacket(payload);
    }
    #endregion

    #region Send Packet
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

    /// <Summary>
    ///     Send the time packet to the device
    /// </summary>
    byte[] TimePacket()
    {
        //byte[] buffer = { 0x02, 0x02, 0x02, 0x54, 0x01, 0x01, 0x00, 0x00, 0x00 };
        var now = DateTime.Now;
        byte[] buffer = {
        0x02,
        0x02,
        (byte)(now.DayOfWeek + 1),
        0x54,               //Year: 84 
        (byte)now.Month,    //Month 
        (byte)now.Day,      //Day
        (byte)now.Hour,     //Hour
        (byte)now.Minute,   //Minute
        (byte)now.Second    //Second
    };
        return NABU.SliceRaw(Logger, 0, Message.TimePak, buffer);
    }

    /// <summary>
    ///     Send a packet to the device
    /// </summary>
    void SendPacket(byte[] buffer, bool escape = true)
    {
        if (buffer.Length > Constants.MaxPacketSize)
        {
            Error("Packet too large");
            Send(ServiceStatus.Unauthorized);
            return;
        }
        Log($"NA: {nameof(ServiceStatus.Authorized)}, NPC: {nameof(Message.ACK)}");
        Send(ServiceStatus.Authorized);               //Prolog
        Recv(Message.ACK);
        if (escape)
            buffer = EscapeBytes(buffer).ToArray();
        Log($"NA: Sending Packet, {buffer.Length} bytes");
        var start = DateTime.Now;
        Send(buffer);
        var stop = DateTime.Now;
        Send(Message.Finished);                      //Epilog
        Task.Run(TransferRatePrinter(start, stop, buffer.Length));

    }

    Action TransferRatePrinter(DateTime start, DateTime stop, int length)
        => () => {
            var elapsed = stop - start;
            var rate = ((length * 8) / elapsed.TotalMilliseconds);
            rate = (length < 100 ?
                            rate * 100 :
                            rate * 1000
                    ) / 1024;
            var unit = "KBps";
            if (rate > 1024)
            {
                rate = rate / 1024;
                unit = "MBps";
            }
            Log($"NPC: Transfer Rate: {rate:0.00} {unit}");
        };

    #endregion

    #region RetroNET

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
        Send(0x00);
        return;
    }

    #endregion

    public virtual async Task<bool> NabuNetHandler(CancellationToken cancel, byte incoming)
    {
        switch (incoming)
        {
            #region Base Messages
            case 0:
                Log($"NA: Received 0, Disconnected");
                return false;
            case Message.Reset:
                Send(Message.ACK);
                Log($"NPC: {nameof(Message.Reset)}, NA: {nameof(Message.ACK)} {nameof(StateMessage.Confirmed)}");
                Send(StateMessage.Confirmed);
                break;
            case Message.MagicalMysteryMessage:
                Send(Message.ACK);
                Log($"NPC: {nameof(Message.MagicalMysteryMessage)}: {Format(Recv(2))}, NA: {nameof(StateMessage.Confirmed)}");
                Send(StateMessage.Confirmed);
                break;
            case Message.GetStatus:
                Send(Message.ACK);
                Log($"NPC: {nameof(Message.GetStatus)}, NA: {nameof(Message.ACK)}");
                GetStatus();
                break;
            case Message.StartUp:
                Send(Message.ACK);
                Log($"NPC: {nameof(Message.StartUp)}, NA: {nameof(Message.ACK)}");
                Send(StateMessage.Confirmed);
                Log($"NA: {nameof(StateMessage.Confirmed)}");
                break;
            case Message.PacketRequest:
                Send(Message.ACK);
                Log($"NPC: {nameof(Message.PacketRequest)}, NA: {nameof(Message.ACK)}");
                await SegmentRequest();
                break;
            case Message.ChangeChannel:
                Send(Message.ACK);
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
                Warning($"Unsupported Message: {Format(incoming)}");
                break;
        }
        return true;
    }
}
