using Microsoft.Extensions.Logging;
using Nabu.Services;
using System.Text;

namespace Nabu.Adaptor;

public abstract class Protocol : NabuService, IProtocol
{
    protected AdaptorSettings Settings { get; private set; }
    protected Stream Stream { get; private set; } = Stream.Null;
    protected BinaryReader Reader { get; private set; }
    protected BinaryWriter Writer { get; private set; }
    protected abstract byte Version { get; }
    protected string Id { get; }

    public abstract string Name { get; }
    public abstract byte Identifier { get; }
    public bool Attached => Stream != Stream.Null;
    
    int SendDelay = 0;
    protected Protocol(
        ILogger logger
    ) : base(logger)
    {
        Settings = new();
        Reader = new BinaryReader(Stream, Encoding.ASCII);
        Writer = new BinaryWriter(Stream, Encoding.ASCII);
        Id = $"{Name} v{Version}";
    }


    #region Send / Receive
    // These methods perform all the stream reading / writing
    // for all communication with the NABU PC / Emulator
    public byte Recv()
    {
        return Reader.ReadByte();
    }

    public (bool, byte) Recv(byte byt)
    {
        var (expected, buffer) = Recv(new[] { byt });
        return (expected, buffer[0]);
    }

    public byte[] FasterRead(int length = 1)
    {
        return Reader.ReadBytes(length);
    }

    public byte[] Recv(int length = 1)
    {
        var buffer = new byte[length];
        for (int i = 0; i < length; i++)
            buffer[i] = Recv();

        Trace($"NA: RCVD: {FormatSeperated(buffer)}");
        Debug($"NA: RCVD: {buffer.Length} bytes");
        return buffer;
    }

    public (bool, byte[]) Recv(params byte[] bytes)
    {
        var read = Recv(bytes.Length);

        var expected = bytes.SequenceEqual(read);
        if (expected is false)
            Warning($"NA: {FormatSeperated(bytes)} != {FormatSeperated(read)}");

        return (
            expected,
            read
        );
    }

    public void Send(params byte[] bytes)
    {
        //SlowerSend(bytes);
        Trace($"NA: SEND: {FormatSeperated(bytes)}");
        Writer.Write(bytes, 0, bytes.Length);
        Debug($"NA: SENT: {bytes.Length} bytes");
    }

    
    public void SlowerSend(params byte[] bytes)
    {
        Trace($"NA: SEND: {FormatSeperated(bytes)}");
        for (int i = 0; i < bytes.Length; i++)
        {
            Writer.Write(bytes[i]);
            Thread.SpinWait(SendDelay);
        }
        Debug($"NA: SENT: {bytes.Length} bytes");
    }
    

    public IEnumerable<byte> Combine(params IEnumerable<byte>[] buffers)
    {
        foreach (var buffer in buffers)
            foreach (var b in buffer)
                yield return b;
    }

    public IEnumerable<byte> String(string str)
    {
        yield return (byte)str.Length;
        foreach (byte b in Encoding.ASCII.GetBytes(str))
            yield return b;
    }

    #endregion


    public virtual bool Attach(AdaptorSettings settings, Stream stream)
    {
        if (Attached) return false;
        else if (
            stream.CanRead is false &&
            stream.CanWrite is false
        ) return false;

        Stream = stream;
        Settings = settings;
        SendDelay = settings.SendDelay ?? 0;
        Reader = new BinaryReader(Stream, Encoding.ASCII);
        Writer = new BinaryWriter(Stream, Encoding.ASCII);
        return true;
    }

    public abstract void Listening();
    public abstract Task<bool> Listen(byte unhandled);
    
    public async Task<bool> Listen(CancellationToken cancel, byte incoming)
    {
        Listening();
        Debug($"Started {Id}");
        
        try
        {
            while (await Listen(incoming))
                continue;

            Debug($"End {Id}");
            return true;
        }
        catch (TimeoutException) {
            return true;
        }
        catch (Exception ex)
        {
            Error($"FAIL {Id}: {ex.Message}");
            return false;
        }

    }

    public void Detach()
    {
        Settings = new();
        Stream = Stream.Null;
        Reader = new BinaryReader(Stream);
        Writer = new BinaryWriter(Stream);
    }
}

