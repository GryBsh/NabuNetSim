using Microsoft.Extensions.Logging;
using System;

namespace Nabu.Binary;

public abstract class BinaryAdapter : IBinaryAdapter
{
    protected Stream? Stream { get; set; }
    protected readonly ILogger Logger;
    public virtual bool Connected => Stream is not null && Stream.Length > 0;
    public BinaryAdapter(ILogger logger)
    {
        Logger = logger;
    }

    #region Communication
    public abstract void Open();
    public abstract void Close();

    protected static string Format(params byte[] bytes) => Tools.Format(bytes);

    public virtual byte[] Recv(int length)
    {
        var bytes = new byte[length];
        
        for (int i = 0; i < length; i++)
        {
            Stream?.Read(bytes, i, 1);
        }
        
        
        //#if DEBUG
        //Logger.LogTrace($"RCVD: {Format(bytes)}");
        //#endif

        Logger.LogDebug($"RCVD: {bytes.Length} bytes");
        return bytes;
    }

    public (bool, byte[]) Recv(params byte[] bytes)
    {
        var read = Recv(bytes.Length);

        var expected = bytes.SequenceEqual(read);
        if (expected is false)
            Logger.LogWarning($"{Format(bytes)} != {Format(read)}");

        return (
            expected,
            read
        );
    }

    public byte Recv() => Recv(1)[0];
    public (bool, byte) Recv(byte byt)
    {
        var read = Recv();
        var expected = read == byt;

        if (expected is false)
            Logger.LogWarning($"{Format(byt)} != {Format(read)}");

        return (
            expected,
            byt
        );
    }

    public virtual void Send(params byte[] bytes)
    {
        Logger.LogTrace($"SEND: {Format(bytes)}");
        Stream?.Write(bytes, 0, bytes.Length);
        Logger.LogDebug($"SENT: {bytes.Length} bytes");
    }

    public void Send(byte[] buffer, int bytes)
        => Send(buffer.Take(bytes).ToArray());

    #endregion 
}
