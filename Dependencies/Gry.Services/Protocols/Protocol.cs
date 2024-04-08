using Gry;
using Gry.Adapters;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Text;

namespace Gry.Protocols;
public abstract class Protocol<TOptions> : IProtocol<TOptions>, IDisposable, IAdaptorProxy
    where TOptions : AdapterDefinition
{
    private bool disposedValue;

    public Protocol(ILogger logger)
    {
        Logger = logger;
        Reader = new BinaryReader(Stream, Encoding.ASCII);
        Writer = new BinaryWriter(Stream, Encoding.ASCII);
    }

    protected ILogger Logger { get; }
    protected string? Label { get; set; }
    public bool Attached => Stream != Stream.Null;
    public abstract byte[] Messages { get; }
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

    public virtual int ReadInt() => Bytes.ToInt(Read(4));

    public virtual ushort ReadShort() => Bytes.ToUShort(Read(2));


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
        var byteLength = Adapter?.Type.Equals(
                            AdapterType.Serial, 
                            StringComparison.OrdinalIgnoreCase
                        ) is true ? 11 : 8;

        var elapsed = stop - start;
        var bitLength = byteLength * length;
        // ([8 or 11] * length) = total bits
        // total bits / seconds = bits / second
        // (b/s) / 1000 = kb/s
        var rate = (bitLength / elapsed.TotalSeconds)/1000;

        var unit = "kb";
        var bUnit = "KB";
        if (rate > 1000)
        {
            rate /= 1000;
            unit = "mb";
            bUnit = "MB";
        }
        var byteUnit = unit == "mb" ? "MB" : "KB";
        var lengthBytes = byteUnit is "KB" ? length/1024 : length/1024/1024; 
        Log($"NA: SENT: {lengthBytes:0.00}{bUnit} ({bitLength/1000}{unit}) in {elapsed.TotalSeconds:0.00} seconds {(rate/8):0.00}{byteUnit}/s ({rate:0.00}{unit}/s)");
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
            return new(0, new());
        }
        else if (length is 0)
        {
            Warning($"0 length frame, aborting.");
            return new(0, new());
        }
        var buffer = Read(length);
        return new(length, buffer);
    }

    public void WriteFrame(Memory<byte> buffer)
    {
        var length = (ushort)buffer.Length;
        var frame = Bytes.Frame(Bytes.FromUShort(length), buffer);
        Write(frame.ToArray());
    }

    public void WriteFrame(byte header, params Memory<byte>[] buffer)
    {
        var head = new byte[] { header };
        var frame = Bytes.Frame(head, buffer);
        WriteFrame(frame);
    }

    public void WritePrefixedFrame(byte prefix, Memory<byte> buffer)
    {
        Write(prefix);
        WriteFrame(buffer);
    }

    #endregion Framed Protocols

    #region IProtocol

    public TOptions? Adapter { get; protected set; }

    public virtual bool Attach(TOptions settings, Stream stream)
    {
        if (Attached) return false;
        else if (
            stream.CanRead is false &&
            stream.CanWrite is false
        ) return false;

        Stream = stream;
        Adapter = settings;
        Reader = new BinaryReader(Stream, Encoding.ASCII);
        Writer = new BinaryWriter(Stream, Encoding.ASCII);
        return true;
    }

    public virtual void Detach()
    {
        Reset();
        Adapter = null;
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

    public virtual bool ShouldHandle(byte unhandled)
    {
        return Messages.Contains(unhandled);
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

    protected string LogMessage(string message)
    {
        var label = Label is null ? string.Empty : $"{Label}: ";
        return $"{label}{Adapter?.Port}: {message}";
    }

    #region Format Bytes

    protected static string Format(byte byt) => Bytes.Format(byt);

    protected static string FormatSeparated(params byte[] bytes) => Bytes.FormatSeparated('|', bytes);

    protected static string FormatTriple(int triple) => $"{triple:X06}";

    #endregion

    #region Logging

    protected void Debug(string message) => Task.Run(() => Logger.LogDebug(LogMessage(message)));

    protected void Error(string message) => Task.Run(() => Logger.LogError(LogMessage(message)));

    protected void Log(string message) => Task.Run(() => Logger.LogInformation(LogMessage(message)));

    protected void Trace(string message) => Task.Run(() => Logger.LogTrace(LogMessage(message)));

    protected void Warning(string message) => Task.Run(() => Logger.LogWarning(LogMessage(message)));

    #endregion
}