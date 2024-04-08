namespace Gry;

public static partial class Bytes
{

    /// <summary>
    ///     Pops the first byte from the provided buffer
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static (int, byte) Pop(Memory<byte> buffer, int offset)
    {
        (int next, buffer) = Slice(buffer, offset, 1);
        return (next, buffer.Span[0]);
    }

    /// <summary>
    ///     Slices the provided buffer and passes the result to the provided converter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    public static (int, T) Slice<T>(Memory<byte> buffer, int offset, int count, Func<Memory<byte>, T> converter)
    {
        (int next, buffer) = Slice(buffer, offset, count);
        return (next, converter(buffer.ToArray()));
    }

    /// <summary>
    ///     Slices the provided buffer
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static (int, Memory<byte>) Slice(Memory<byte> buffer, int offset, int length)
    {
        int next = offset + length;
        if (next >= buffer.Length)
        {
            next = 0;
            length = buffer.Length - offset;
        }
        return (next, buffer.Slice(offset, length).ToArray());
    }
            

    public static Memory<byte> Frame(Memory<byte> header, params Memory<byte>[] buffer)
    {
        return Concat(header, Concat(buffer));
    }
}
