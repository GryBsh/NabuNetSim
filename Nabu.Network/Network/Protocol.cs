using Nabu.Services;
using System.Reactive.Linq;
using System.Text;

namespace Nabu.Network;

public abstract class Protocol : NabuService, IProtocol, IDisposable, IAdaptorProxy
{
    private bool disposedValue;

    private int SendDelay = 0;

    public Protocol(
            ILog logger,
            AdaptorSettings? settings = null
        ) : base(logger, settings ?? new NullAdaptorSettings())
    {
        Reader = new BinaryReader(Stream, Encoding.ASCII);
        Writer = new BinaryWriter(Stream, Encoding.ASCII);
    }

    public bool Attached => Stream != Stream.Null;

    public abstract byte[] Commands { get; }

    public BinaryReader Reader { get; private set; }
    public Stream Stream { get; private set; } = Stream.Null;
    public abstract byte Version { get; }
    public BinaryWriter Writer { get; private set; }

    #region Values

    protected byte Byte(int number)
    {
        return (byte)number;
    }

    #endregion Values

    #region Send / Receive

    // These methods perform all the stream reading / writing
    // for all communication with the NABU PC / Emulator

    /// <summary>
    ///     Receives a byte and returns if it was the expected byte
    ///     and the actual byte received.
    /// </summary>
    /// <param name="expected">The expected byte</param>
    /// <returns>If it was the expected byte and the actual byte received</returns>
    public virtual AdaptorResult<byte> Read(byte expected)
    {
        var read = Read();
        var good = read == expected;
        if (!good) Warning($"NA: {Format(expected)} != {Format(read)}");
        return new(good, read);
    }

    /// <summary>
    ///     Receive 1 byte.
    /// </summary>
    /// <returns>The received byte</returns>
    public virtual byte Read()
    {
        var b = Reader.ReadByte();
        Trace($"NA: RCVD: {Format(b)}");
        Debug($"NA: RCVD: 1 byte");
        return b;
    }

    /// <summary>
    ///     Receives the specified bytes.
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public virtual byte[] Read(int length)
    {
        var buffer = Reader.ReadBytes(length);
        Trace($"NA: RCVD: {FormatSeparated(buffer)}");
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
    public virtual AdaptorResult<byte[]> Read(params byte[] expected)
    {
        var read = Read(expected.Length);
        var good = expected.SequenceEqual(read);
        if (!good) Warning($"NA: {FormatSeparated(expected)} != {FormatSeparated(read)}");
        return new(good, read);
    }

    public virtual async Task<Memory<byte>> ReadAsync(int length)
    {
        var buffer = new Memory<byte>(new byte[length]);
        await Stream.ReadAsync(buffer);
        return buffer;
    }

    public virtual async Task<byte> ReadAsync() => (await ReadAsync(1)).Span[0];

    public virtual int ReadInt() => NabuLib.ToInt(Read(4));

    public virtual ushort ReadShort() => NabuLib.ToUShort(Read(2));

    /// <summary>
    ///     Provides a method to send data to the NABU PC / Emulator
    ///     at a reduced speed.
    /// </summary>
    /// <param name="bytes">The bytes to send</param>
    public void SlowerSend(params byte[] bytes)
    {
        Trace($"NA: SEND: {FormatSeparated(bytes)}");
        for (int i = 0; i < bytes.Length; i++)
        {
            Writer.Write(bytes[i]);
            Thread.SpinWait(SendDelay);
        }
        Debug($"NA: SENT: {bytes.Length} bytes");
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
    public virtual void Write(params byte[] bytes)
    {
        Trace($"NA: SEND: {FormatSeparated(bytes)}");

        //if (bytes.Length > 128)
        //{
        //    DateTime start = DateTime.Now;
        //    Writer.Write(bytes);

        //    DateTime end = DateTime.Now;
        //    TransferRate(start, end, bytes.Length);
        //}
        //else
        //{
        Writer.Write(bytes);
        //}
        Writer.Flush();
        Debug($"NA: SENT: {bytes.Length} bytes");
    }

    /// <summary>
    ///     Logs the current transfer rate
    /// </summary>
    /// <param name="start">The start time</param>
    /// <param name="stop">The end time</param>
    /// <param name="length">The number of bytes transfered</param>
    protected void TransferRate(DateTime start, DateTime stop, int length)
    {
        // The RS422 connection has 1 start and 2 stop bits
        var byteLength = Adaptor is SerialAdaptorSettings ? 11 : 8;
        var elapsed = stop - start;

        // ND: I have no idea why I had `- length` in here...
        //var rate = (byteLength * length - length) / (elapsed.TotalMilliseconds / 1000) / 1000;
        var rate = byteLength * length / (elapsed.TotalMilliseconds / 1000) / 1000;

        var unit = "kb/s";
        if (rate > 1000)
        {
            rate /= 1000;
            unit = "mb/s";
        }
        Log($"NPC: Transfer Rate: {rate:0.00} {unit} in {elapsed.TotalSeconds:0.00} seconds");
    }

    #endregion Send / Receive

    #region Framed Protocols

    public AdaptorFrame ReadFrame()
    {
        //var ln = Read(2);
        var length = ReadShort();
        if (0 > length)
        {
            Warning($"NabuNet message detected in frame, aborting.");
            return new(0, Array.Empty<byte>());
        }
        else if (length is 0)
        {
            Warning($"0 length frame, aborting.");
            return new(0, Array.Empty<byte>());
        }
        var buffer = Read(length);
        return new(length, buffer);
    }

    public void WriteFrame(params byte[] buffer)
    {
        var length = (ushort)buffer.Length;
        var frame = NabuLib.Frame(NabuLib.FromUShort(length), buffer);
        Write(frame.ToArray());
    }

    public void WriteFrame(byte header, params Memory<byte>[] buffer)
    {
        var head = new byte[] { header };
        var frame = NabuLib.Frame(head, buffer);
        WriteFrame(frame.ToArray());
    }

    #endregion Framed Protocols

    #region IProtocol

    public virtual bool Attach(AdaptorSettings settings, Stream stream)
    {
        if (Attached) return false;
        else if (
            stream.CanRead is false &&
            stream.CanWrite is false
        ) return false;

        Stream = stream;
        Adaptor = settings;
        Reader = new BinaryReader(Stream, Encoding.ASCII);
        Writer = new BinaryWriter(Stream, Encoding.ASCII);
        return true;
    }

    public virtual void Detach()
    {
        Reset();
        Adaptor = new NullAdaptorSettings();
        Stream = Stream.Null;
        Reader = new BinaryReader(Stream);
        Writer = new BinaryWriter(Stream);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task<bool> HandleMessage(byte incoming, CancellationToken cancel)
    {
        try
        {
            await Handle(incoming, cancel);
            return true;
        }
        catch (TimeoutException)
        {
            return true;
        }
        catch (Exception ex)
        {
            Error($"FAIL: {ex.Message}");
            return false;
        }
    }

    public virtual void Reset()
    { }

    public virtual bool ShouldAccept(byte unhandled)
    {
        return Commands.Contains(unhandled);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Detach();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    protected abstract Task Handle(byte unhandled, CancellationToken cancel);

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Protocol()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    #endregion IProtocol
}