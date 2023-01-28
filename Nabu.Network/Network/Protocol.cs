using Microsoft.Extensions.Logging;
using System.Text;
using System;

namespace Nabu.Adaptor;

public abstract class Protocol : NabuService, IProtocol
{
    //protected AdaptorSettings Settings { get; private set; }
    protected Stream Stream { get; private set; } = Stream.Null;
    protected BinaryReader Reader { get; private set; }
    protected BinaryWriter Writer { get; private set; }
    protected abstract byte Version { get; }
    public abstract byte[] Commands { get; } 
    public bool Attached => Stream != Stream.Null;
    
    int SendDelay = 0;
    protected Protocol(
        ILogger logger
    ) : base(logger, new NullAdaptorSettings())
    {
        
        Reader = new BinaryReader(Stream, Encoding.ASCII);
        Writer = new BinaryWriter(Stream, Encoding.ASCII);
        
    }


    #region Send / Receive
    // These methods perform all the stream reading / writing
    // for all communication with the NABU PC / Emulator

    /// <summary>
    ///     Receive 1 byte.
    /// </summary>
    /// <returns>The received byte</returns>
    protected byte Recv()
    {
        var b = Reader.ReadByte();
        Trace($"NA: RCVD: {Format(b)}");
        Debug($"NA: RCVD: 1 byte");
        return b;
    }

    protected async Task<byte[]> RecvAsync(int length) 
    {
        var buffer = new byte[length];
        await Stream.ReadAsync(buffer.AsMemory(0, length));
        return buffer;
    }

    protected async Task<byte> RecvAsync() => (await RecvAsync(1))[0];

    /// <summary>
    ///     Receives a byte and returns if it was the expected byte
    ///     and the actual byte received.
    /// </summary>
    /// <param name="expected">The expected byte</param>
    /// <returns>If it was the expected byte and the actual byte received</returns>
    protected (bool, byte) Recv(byte expected)
    {
        var read = Recv();
        var good = read == expected;
        if (!good) Warning($"NA: {Format(expected)} != {Format(read)}");
        return (good, read);
    }

    public int RecvInt() => NabuLib.ToInt(Reader.ReadBytes(4));
    public short RecvShort() => NabuLib.ToShort(Reader.ReadBytes(2));

    public string RecvString() => Reader.ReadString();

    /// <summary>
    ///     Receives the specified bytes.
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    protected byte[] Recv(int length)
    {
        var buffer = Reader.ReadBytes(length);
        Trace($"NA: RCVD: {FormatSeperated(buffer)}");
        Debug($"NA: RCVD: {buffer.Length} bytes");
        return buffer;
    }
        
    /// <summary>
    ///     Receives the number of bytes expected
    ///     and returns if the bytes received are equal
    ///     as well as the actual bytes received.
    /// </summary>
    /// <param name="expected">The bytes expected to be received</param>
    /// <returns>If the expected bytes were received and the bytes actually received</returns>
    protected (bool, byte[]) Recv(params byte[] expected)
    {
        var read = Recv(expected.Length);
        var good = expected.SequenceEqual(read);
        if (!good) Warning($"NA: {FormatSeperated(expected)} != {FormatSeperated(read)}");
        return (good, read);
    }  


    /// <summary>
    ///     Logs the current transfer rate
    /// </summary>
    /// <param name="start">The start time</param>
    /// <param name="stop">The end time</param>
    /// <param name="length">The number of bytes transfered</param>
    void TransferRate(DateTime start, DateTime stop, int length)
    {
        var byteLength = Settings.Type switch
        {
            AdaptorType.Serial => 11, // 8 + 1 + 2
            _ => 8
        };
        var elapsed = stop - start;
        var rate = ((byteLength * length) - length) / (elapsed.TotalMilliseconds / 1000) / 1000;
        var unit = "kb/s";
        if (rate > 1000)
        {
            rate /= 1000;
            unit = "mb/s";
        }
        Log($"NPC: Transfer Rate: {rate:0.00} {unit} in {elapsed.TotalSeconds:0.00} seconds");
    }

    /// <summary>
    ///     Sends bytes to the NABU PC / Emulator.
    ///     This method times and logs the transfer rate
    ///     of all transfers larger than 128 bytes.
    /// </summary>
    /// <remarks>
    ///     Anything smaller than 128 bytes sends at an
    ///     impossible speed over serial or TCP. I believe
    ///     thats the size of the underlying
    ///     <ref>System.IO.Stream</ref>'s buffer.
    /// </remarks>
    /// <param name="bytes">The bytes to send</param>
    protected void Send(params byte[] bytes)
    {
        Trace($"NA: SEND: {FormatSeperated(bytes)}");
        
        if (bytes.Length > 128)
        { // This doesn't work unless you fill the buffer
            DateTime start = DateTime.Now;
            //Writer.Write(bytes);
            //Writer.Flush();
            Writer.Write(bytes);
            DateTime end = DateTime.Now;
            TransferRate(start, end, bytes.Length);
        }
        else
        {
            Writer.Write(bytes);
        }
        //Task.Run(Writer.Flush);
        Debug($"NA: SENT: {bytes.Length} bytes");
    }


    /// <summary>
    ///     Provides a method to send data to the NABU PC / Emulator
    ///     at a reduced speed.
    /// </summary>
    /// <param name="bytes">The bytes to send</param>
    protected void SlowerSend(params byte[] bytes)
    {
        Trace($"NA: SEND: {FormatSeperated(bytes)}");
        for (int i = 0; i < bytes.Length; i++)
        {
            Writer.Write(bytes[i]);
            Thread.SpinWait(SendDelay);
        }
        Debug($"NA: SENT: {bytes.Length} bytes");
    }

    #endregion

    #region Framed Protocols
    protected void SendFramed(params byte[] buffer)
    {
        Send(NabuLib.FromShort((short)buffer.Length));
        Send(buffer);
    }



    protected void SendFramed(byte header, params IEnumerable<byte>[] buffer)
    {
        var head = new byte[] { header };
        var frame = NabuLib.Frame(head, buffer);
        SendFramed(frame.ToArray());
    }

    protected (short, byte[]) ReadFrame()
    {
        var ln = Recv(2);
        var length = NabuLib.ToShort(ln);
        if (0 > length)
        {
            Warning($"NabuNet message detected in frame, aborting.");
            return (0, Array.Empty<byte>());
        }
        else if (length is 0)
        {
            Warning($"0 length frame, aborting.");
            return (0, Array.Empty<byte>());
        }
        var buffer = Recv(length);
        return (length, buffer);
    }

    #endregion

    #region IProtocol

    public virtual bool Attach(AdaptorSettings settings, Stream stream)
    {
        if (Attached) return false;
        else if (
            stream.CanRead is false &&
            stream.CanWrite is false
        ) return false;

        Stream = stream;
        Settings = settings;
        Reader = new BinaryReader(Stream, Encoding.ASCII);
        Writer = new BinaryWriter(Stream, Encoding.ASCII);
        return true;
    }

    public abstract Task Handle(byte unhandled, CancellationToken cancel);

    public async Task<bool> Listen(byte incoming, CancellationToken cancel)
    { 
        try
        {
            
            await Handle(incoming, cancel);
            
            return true;
        }
        catch (TimeoutException) {
            return true;
        }
        catch (Exception ex)
        {
            Error($"FAIL: {ex.Message}");
            return false;
        }
    }

    public virtual void Detach()
    {
        Settings = new NullAdaptorSettings();
        Stream = Stream.Null;
        Reader = new BinaryReader(Stream);
        Writer = new BinaryWriter(Stream);
    }

    #endregion

    public virtual void Reset() { }

    public virtual bool ShouldAccept(byte unhandled)
    {
        return Commands.Contains(unhandled);    
    }
}

