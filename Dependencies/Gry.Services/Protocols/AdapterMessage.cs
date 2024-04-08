using Gry;
using System;
using System.Runtime.CompilerServices;

namespace Gry.Protocols;

public abstract record AdapterMessage : AdapterMessage<byte, byte>
{
    protected AdapterMessage() { }
    protected AdapterMessage(Memory<byte> data)
    {
        Type = data.Span[0];
        Body = data[1..];
    }

    /// <summary>
    /// Reads a size prefixed ASCII string from the Body
    /// </summary>
    /// <param name="start">The location to start reading from</param>
    /// <returns>The string read</returns>
    protected string SizedASCII(ushort start)
    {
        var first = start + 1;
        return FromASCII((ushort)first, Body.Span[start]);
    }

    /// <summary>
    /// Reads a string from the body. Either to the specified length,
    /// or NULL is read.
    /// </summary>
    /// <param name="start">The location to start reading from</param>
    /// <param name="length">The expected number of bytes</param>
    /// <returns>The string read</returns>
    protected string FromASCII(ushort start, byte length)
    {
        var buffer = Body[start..(start + length)];
        var str = string.Empty;
        for (int j = 0; j < buffer.Length; j++)
        {
            if (buffer.Span[j] is 0x00) break;
            str += (char)buffer.Span[j];
        }
        return str;
    }


    protected void ThrowIfBadType(byte expected)
    {
        if (expected != Type)
            throw new InvalidDataException(
                      $"Type `{Type}` is not `{expected}`"
                  );
    }

    protected static void ThrowIf<TEx>(Func<bool> predicate, TEx ex)
        where TEx : Exception
    {
        if (!predicate()) return;
        throw ex;
    }
}

public abstract record AdapterMessage<TType, TBody>
{
    public TType Type { get; protected init; }
    public Memory<TBody> Body { get; protected init; }

}
