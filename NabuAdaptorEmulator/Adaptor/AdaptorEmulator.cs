using Microsoft.Extensions.Logging;
using Nabu.Binary;
using Nabu.Network;
using Nabu.Services;
using System.Diagnostics;

namespace Nabu.Adaptor;

public abstract class AdaptorEmulator : NabuEmulator
{
    readonly IBinaryAdapter Adapter;
    readonly AdaptorSettings Settings;
    AdaptorState State;
    readonly NetworkEmulator Network;
    public AdaptorEmulator(
        NetworkEmulator network,
        ILogger logger,
        AdaptorSettings settings,
        IBinaryAdapter serial
    ) : base(logger)
    {
        Network = network;
        Adapter = serial;
        Settings = settings;
        State = new();
        
    }

    #region Communication
    public void Open(AdaptorSettings settings)
    {
        State = new(){
            Channel = settings.AdapterChannel,
            ChannelKnown = !settings.ChannelPrompt
        };
        Adapter.Open();
        Network.SetState(settings);
    }

    public void Close()
    {
        Adapter.Close();
    }

    public byte Recv()
    {
        return Adapter.Recv();
    }

    public (bool, byte) Recv(byte byt)
    {
        return Adapter.Recv(byt);
    }

    public byte[] Recv(int length = 1)
    {
        return Adapter.Recv(length);
    }

    public (bool, byte[]) Recv(params byte[] bytes)
    {
        return Adapter.Recv(bytes);
    }

    public void Send(params byte[] bytes)
    {
        Adapter.Send(bytes);
    }

    public void Send(byte[] buffer, int bytes)
    {
        Adapter.Send(buffer, bytes);
    }
    #endregion

    #region Channel Status / Change
    void ChannelStatus()
    {
        if (!State.ChannelKnown)
        {
            Log("Adaptor: Requesting Channel Code");
            Send(Messages.RequestChannelCode);
            return;
        }

        Log($"Adaptor: Channel Code Set: {State.Channel:X04}");
        Send(Messages.ConfirmChannelCode);
    }

    void ChannelChange()
    {

        short data = Tools.PackShort(Adapter.Recv(2));
        if (data < 0x100)
        {
            State.Channel = data;
            State.ChannelKnown = true;
        }
        else
        {
            State.Channel = 0;
            State.ChannelKnown = false;
            return;
        }
        Send(Messages.Confirmed);
        Log($"Channel Code: {State.Channel:X04}");
    }
    #endregion

    #region Packet Request
    async Task PacketRequest()
    {
        short packet = Recv();
        int segment = Tools.PackInt(Recv(3));
        Log($"Packet: {packet:x04}, Segment: {Tools.FormatTriple(segment)}");
        Log($"Adaptor: {nameof(Messages.Confirmed)}");
        Send(Messages.Confirmed);
        if (packet == 0x00 &&
            segment == Messages.TimeSegment
        )
        {
            Log("NABU: Time?");
            SendTimePacket();
            return;
        }

        // Network Emulator
        var (type, segmentData) = await Network.Request(segment);
        if (type is ImageType.None)
        {
            Error("No Image Found or Error");
            Send(Messages.Unauthorized);
            return;
        }
        if (type == ImageType.Nabu)
            await Task.Run(() => PreparePacket(packet, segment, segmentData));
        else
            await Task.Run(() => SlicePacket(packet, segmentData));
    }
    #endregion

    #region Packet Format

    void SlicePacket(short packet, byte[] buffer)
    {
        int length = Constants.TotalPayloadSize;
        int offset = (packet * length) + ((2 * packet) + 2);
        if (offset + length >= buffer.Length)
        {
            length = buffer.Length - offset;
        }
        var message = buffer.Skip(offset).Take(length).ToArray();

        Debug("Sending Packet from PAK");
        var crc = GenerateCRC(message[0..(length - 2)]);
        message[^2] = (byte)(crc >> 8 & 0xFF ^ 0xFF);    //CRC MSB
        message[^1] = (byte)(crc >> 0 & 0xFF ^ 0xFF);    //CRC LSB
        SendPacket(message);
    }

    void PreparePacket(
        short packet,
        int segment,
        byte[] buffer)
    {
        int offset = packet * Constants.MaxPayloadSize;
        if (offset >= buffer.Length)
        {
            Error($"Packet Start {offset} is beyond the end of the buffer");
            Send(Messages.Unauthorized);
            return;
        }

        Trace("Preparing Packet");

        int length = Constants.MaxPayloadSize;
        bool lastPacket = false;
        if (offset + length >= buffer.Length)
        {
            length = buffer.Length - offset;
            lastPacket = true;
        }
        int packetSize = length + Constants.HeaderSize + Constants.FooterSize;
        int lastIndex = offset + length - 1;

        var message = new byte[packetSize];
        int idx = 0;
        // 16 bytes of header
        message[idx++] = (byte)(segment >> 16 & 0xFF);          //Segment MSB   
        message[idx++] = (byte)(segment >> 8 & 0xFF);           //              
        message[idx++] = (byte)(segment >> 0 & 0xFF);           //Segment LSB   
        message[idx++] = (byte)(packet & 0xff);                 //Packet LSB    
        message[idx++] = 0x01;                                  //Owner         
        message[idx++] = 0x7F;                                  //Tier MSB      
        message[idx++] = 0xFF;
        message[idx++] = 0xFF;
        message[idx++] = 0xFF;                                  //Tier LSB
        message[idx++] = 0x7F;                                  //Mystery Byte
        message[idx++] = 0x80;                                  //Mystery Byte
        message[idx++] = (byte)(                                //Packet Type
                            (lastPacket ? 0x10 : 0x00) |        //bit 4 (0x10) marks End of Segment
                            (packet == 0 ? 0xA1 : 0x20)
                         );
        message[idx++] = (byte)(packet >> 0 & 0xFF);            //Packet # LSB
        message[idx++] = (byte)(packet >> 8 & 0xFF);            //Packet # MSB
        message[idx++] = (byte)(offset >> 8 & 0xFF);            //Offset MSB
        message[idx++] = (byte)(offset >> 0 & 0xFF);            //Offset LSB
        // Data
        for (int i = offset; i <= lastIndex; i++)
        {
            message[idx++] = buffer[i];                         //DATA
        }
        //CRC Footer
        var crc = GenerateCRC(message[0..idx]);
        message[idx++] = (byte)(crc >> 8 & 0xFF ^ 0xFF);    //CRC MSB
        message[idx++] = (byte)(crc >> 0 & 0xFF ^ 0xFF);    //CRC LSB

        Debug("Sending Packet");
        SendPacket(message);
        if (lastPacket) GC.Collect();
    }

    short GenerateCRC(byte[] buffer)
    {
        short crc = -1; // 0xFFFF
        foreach (var byt in buffer)
        {
            byte b = (byte)(crc >> 8 ^ byt);
            crc <<= 8;
            crc ^= Constants.CRCTable[b];
        }
        return crc;
    }
    #endregion

    #region Send Packet
    void SendTimePacket()
    {
        byte[] buffer = { 0x02, 0x02, 0x02, 0x54, 0x01, 0x01, 0x00, 0x00, 0x00 };
        PreparePacket(0, Messages.TimeSegment, buffer);
    }

    void SendPacket(byte[] buffer, bool escape = true)
    {
        if (buffer.Length > Constants.MaxPacketSize)
        {
            Error("Packet too large");
            Send(Messages.Unauthorized);
            return;
        }

        Send(Messages.Authorized);               //Prolog
        Recv(Messages.ACK);

        Log($"Sending Packet, {buffer.Length} bytes");
        var timer = Stopwatch.StartNew();
        if (escape)
            buffer = EscapeBytes(buffer).ToArray();
        Send(buffer);
        timer.Stop();

        Send(Messages.End);                      //Epilog
        Task.Run(() =>
        {
            var rate = $"{buffer.Length * 8 / 1000 / (timer.Elapsed.TotalMilliseconds / 1000):0.00}";
            if (rate is not "0.00") State.LastRate = rate;
            else rate = State.LastRate;
            Trace($"{rate} Kbps");
        });
    }
    #endregion

    #region Adaptor Loop
    public async Task Emulate(CancellationToken cancel)
    {
        
        while (cancel.IsCancellationRequested is false)
        {            
            try
            {
                if (Adapter.Connected is false) {
                    await Task.Delay(1000, cancel);
                    continue;
                }
                Log("Waiting for NABU");
                byte incoming = Recv();

                switch (incoming)
                {
                    #region Messages
                    case 0:
                        goto END;
                    case 0xFF:
                        continue; // They were in DKG's code, so I've kept them
                    case 0xEF:
                        continue; // I have not seen enough packets to know why.
                    case Messages.ChannelStatus:
                        Log("NABU: Channel Status?");
                        ChannelStatus();
                        continue;
                    case Messages.Ready:
                        Log($"NABU: {nameof(Messages.Ready)}, Adaptor: {nameof(Messages.Confirmed)}");
                        Send(Messages.Confirmed);
                        continue;
                    case 0x0F:
                        continue; // So I defer to his expertise
                    case 0x1C:
                        continue; // also unknown
                    case 0x1E:
                        Log($"NABU: 1E, Adaptor: {nameof(Messages.End)}");
                        Send(Messages.End);
                        continue;
                    case Messages.Wait:
                        Log($"NABU: Wait, Adaptor: {nameof(Messages.ACK)}");
                        Send(Messages.ACK);
                        continue;
                    case Messages.StartingUp:
                        Log($"NABU: {nameof(Messages.StartingUp)}, Adaptor: {nameof(Messages.ACK)}");
                        Send(Messages.ACK);
                        continue;
                    case Messages.Initialize:
                        Log($"NABU: {nameof(Messages.Initialize)}, Adaptor: {nameof(Messages.Initialized)}");
                        Send(Messages.Initialized);
                        continue;
                    case Messages.PacketRequest:
                        Log($"NABU: {nameof(Messages.PacketRequest)}, Adaptor: {nameof(Messages.ACK)}");
                        Send(Messages.ACK);
                        await PacketRequest();
                        continue;
                    case Messages.ChangeChannel:
                        Log($"NABU: {nameof(Messages.ChangeChannel)}, Adaptor: {nameof(Messages.ACK)} ");
                        Send(Messages.ACK);
                        ChannelChange();
                        continue;
                    case 0x8F:
                        continue; // and this
                    #endregion

                    default:
                        Trace($"UNEXPECTED: {incoming}");
                        continue;
                }
            }
            catch (TimeoutException)
            {
                Trace("Timeout expired.");
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return;
            }
            
            GC.Collect();
        }

    END:
        Log("Ending Adaptor Loop");
        GC.Collect();
    }
    #endregion

}


