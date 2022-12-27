using Microsoft.Extensions.Logging;
using Nabu.Binary;
using Nabu.Network;
using Nabu.Services;
using System;
using System.Diagnostics;

namespace Nabu.Adaptor;

public abstract class AdaptorEmulator : NabuService
{
    readonly IBinaryAdapter Adapter;
    AdaptorSettings? Settings;
    AdaptorState State;
    readonly NetworkEmulator Network;
    public AdaptorEmulator(
        NetworkEmulator network,
        ILogger logger,
        
        IBinaryAdapter serial
    ) : base(logger)
    {
        Network = network;
        Adapter = serial;
        //Settings = settings;
        State = new();
        
    }

    #region Communication
    public virtual void Open(AdaptorSettings settings)
    {
        Settings = settings;
        State = new(){
            Channel = settings.AdapterChannel
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

    void GetStatus() 
    {
        short data = Tools.PackShort(Adapter.Recv(1));
        
        switch (data)
        {
            case AdaptorStatus.Signal: // <- NA Channel Lock Status
                if (State.ChannelKnown)
                {
                    Log($"NPC: {nameof(AdaptorStatus.Signal)}? NA: {nameof(Status.SignalLock)}");
                    Send(Status.SignalLock);
                    Send(Messages.Finished);
                }
                else {
                    Log($"NPC: {nameof(AdaptorStatus.Signal)}? NA: {nameof(Status.NoSignal)}");
                    Send(Status.NoSignal);
                    Send(Messages.Finished);
                }
                break;
            case 0x1E: // <-- NPC Program/DOS Loaded
                Log($"NPC: 1E? NA: {nameof(Messages.Finished)}");
                Send(Messages.Finished);
                break;
            default:
                Log($"Unsupported Status: {data:X02}");
                break;
        }

    }

    void ChannelChange()
    {
        short data = Tools.PackShort(Adapter.Recv(2));
        if (data is > 0 and < 0x100)
        {
            State.Channel = data;
        }
        else
        {
            State.Channel = 0;
            //State.ChannelKnown = false;
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
        Log($"NPC: Packet: {packet:x04}, Segment: {Tools.FormatTriple(segment)}, NA: {nameof(Messages.Confirmed)}");
        Send(Messages.Confirmed);
        if (packet == 0x00 &&
            segment == Messages.TimePak
        )
        {
            Log("NPC: What Time it is?");
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
            await Task.Run(() => SliceAndSendRaw(packet, segment, segmentData));
        else
            await Task.Run(() => SliceAndSendFromPak(packet, segmentData));
    }
    #endregion

    #region Packet Format
    /// <summary>
    ///     Slices a packet from the given pre-prepared segment pak
    /// </summary>
    void SliceAndSendFromPak(short segment, byte[] buffer)
    {
        int length = Constants.TotalPayloadSize;
        int offset = (segment * length) + ((2 * segment) + 2);
        if (offset >= buffer.Length)
        {
            Error($"Packet Start {offset} is beyond the end of the buffer");
            Send(Messages.Unauthorized);
            return;
        }
        if (offset + length >= buffer.Length)
        {
            length = buffer.Length - offset;
        }
        var message = buffer.Skip(offset).Take(length).ToArray();

        Debug("Sending Packet from PAK");
        var crc = GenerateCRC(message[0..^2]);
        message[^2] = (byte)(crc >> 8 & 0xFF ^ 0xFF);    //CRC MSB
        message[^1] = (byte)(crc >> 0 & 0xFF ^ 0xFF);    //CRC LSB
        SendPacket(message);
    }

    /// <summary>
    ///     Slices the given packet the given buffer of program data
    ///     and creates the packet header and footer structures around it
    /// </summary>
    /// <param name="segment"></param>
    /// <param name="pak"></param>
    /// <param name="buffer"></param>
    void SliceAndSendRaw(
        short segment,
        int pak,
        byte[] buffer)
    {
        int offset = segment * Constants.MaxPayloadSize;
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
        message[idx++] = (byte)(pak >> 16 & 0xFF);              //Pak MSB   
        message[idx++] = (byte)(pak >> 8 & 0xFF);               //              
        message[idx++] = (byte)(pak >> 0 & 0xFF);               //Pak LSB   
        message[idx++] = (byte)(segment & 0xff);                //Segment LSB    
        message[idx++] = 0x01;                                  //Owner         
        message[idx++] = 0x7F;                                  //Tier MSB      
        message[idx++] = 0xFF;
        message[idx++] = 0xFF;
        message[idx++] = 0xFF;                                  //Tier LSB
        message[idx++] = 0x7F;                                  //Mystery Byte
        message[idx++] = 0x80;                                  //Mystery Byte
        message[idx++] = (byte)(                                //Packet Type
                            (lastPacket ? 0x10 : 0x00) |        //bit 4 (0x10) marks End of Segment
                            (segment == 0 ? 0xA1 : 0x20)
                         );
        message[idx++] = (byte)(segment >> 0 & 0xFF);           //Segment # LSB
        message[idx++] = (byte)(segment >> 8 & 0xFF);           //Segment # MSB
        message[idx++] = (byte)(offset >> 8 & 0xFF);            //Offset MSB
        message[idx++] = (byte)(offset >> 0 & 0xFF);            //Offset LSB

        buffer[offset..lastIndex].CopyTo(message, idx);         //DATA
        idx += length;
        
        //CRC Footer
        var crc = GenerateCRC(message[0..idx]);
        message[idx++] = (byte)(crc >> 8 & 0xFF ^ 0xFF);        //CRC MSB
        message[idx++] = (byte)(crc >> 0 & 0xFF ^ 0xFF);        //CRC LSB

        Debug("Sending Packet");
        SendPacket(message);
        if (lastPacket) GC.Collect();
    }

    /// <summary>
    /// Generates the 2 CRC bytes for a given packet buffer
    /// </summary>
    /// <param name="buffer">the contents of the packet</param>
    /// <returns>2 CRC bytes packed into a short</returns>
    static short GenerateCRC(byte[] buffer)
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
    /// <summary>
    ///     Escapes the Escape bytes in the packet with Escape.
    /// </summary>
    static IEnumerable<byte> EscapeBytes(IEnumerable<byte> sequence)
    {
        foreach (byte b in sequence)
        {
            if (b == Messages.Escape)
            {
                yield return Messages.Escape;
                yield return b;
            }
            else
                yield return b;
        }
    }

    /// <Summary>
    ///     Send the time packet to the device
    /// </summary>
    void SendTimePacket()
    {
        //byte[] buffer = { 0x02, 0x02, 0x02, 0x54, 0x01, 0x01, 0x00, 0x00, 0x00 };
        var now = DateTime.Now;
        byte[] buffer = {
            0x02,
            0x02,
            0x02,
            0x54,               //Year: 84 
            (byte)now.Month,    //Month 
            (byte)now.Day,      //Day
            (byte)now.Hour,     //Hour
            (byte)now.Minute,   //Minute
            (byte)now.Second    //Second
        };
        SliceAndSendRaw(0, Messages.TimePak, buffer);
    }

    /// <summary>
    ///     Send a packet to the device
    /// </summary>
    void SendPacket(byte[] buffer, bool escape = true)
    {
        if (buffer.Length > Constants.MaxPacketSize)
        {
            Error("Packet too large");
            Send(Messages.Unauthorized);
            return;
        }
        Log($"NA: {nameof(Messages.Authorized)}, NPC: {nameof(Messages.ACK)}");
        Send(Messages.Authorized);               //Prolog
        Recv(Messages.ACK);

        Log($"NA: Sending Packet, {buffer.Length} bytes");
        if (escape)
            buffer = EscapeBytes(buffer).ToArray();
        var start = DateTime.Now;
        Send(buffer);
        var stop = DateTime.Now;
        Send(Messages.Finished);                      //Epilog
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

    #region Adaptor Loop
    public async Task Emulate(CancellationToken cancel)
    {
        Log("Waiting for NABU");
        while (cancel.IsCancellationRequested is false)
        {            
            try
            {
                if (Adapter.Connected is false) {
                    await Task.Delay(1000, cancel);
                    continue;
                }
               
                byte incoming = Recv();

                switch (incoming)
                {
                    #region NABU Messages
                    case 0:
                        Log($"NA: Received 0, Disconnected");
                        goto END;
                    case Messages.Reset:
                        Log($"NPC: {nameof(Messages.Reset)}, NA: {nameof(Messages.ACK)} {nameof(Messages.Confirmed)}");
                        Send(Messages.ACK);
                        Send(Messages.Confirmed);
                        continue;
                    case Messages.MagicalMysteryMessage:
                        Send(Messages.ACK);
                        Log($"NPC: {nameof(Messages.MagicalMysteryMessage)}: {Format(Recv(2))}, NA: {nameof(Messages.Confirmed)}");
                        Send(Messages.Confirmed);
                        continue;
                    case Messages.GetStatus:
                        Log($"NPC: {nameof(Messages.GetStatus)}, NA: {nameof(Messages.ACK)}");
                        Send(Messages.ACK);
                        GetStatus();
                        continue;
                    case Messages.StartUp:
                        Log($"NPC: {nameof(Messages.StartUp)}, NA: {nameof(Messages.ACK)}");
                        Send(Messages.ACK);
                        Send(Messages.Confirmed);
                        Log($"NA: {nameof(Messages.Confirmed)}");
                        continue;
                    case Messages.PacketRequest:
                        Log($"NPC: {nameof(Messages.PacketRequest)}, NA: {nameof(Messages.ACK)}");
                        Send(Messages.ACK);
                        await PacketRequest();
                        continue;
                    case Messages.ChangeChannel:
                        Log($"NPC: {nameof(Messages.ChangeChannel)}");
                        Send(Messages.ACK);
                        Log($"NA: {nameof(Messages.ACK)}");
                        ChannelChange();
                        continue;
                    #endregion

                    default:
                        Warning($"Unsupported Message: {Format(incoming)}");
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
                goto END;
            }
            
            GC.Collect();
        }

    END:
        Log("Disconnected");
        GC.Collect();
    }
    #endregion

}


