namespace Nabu;

public static partial class NabuLib
{
    /// <summary>
    ///     Pops the first byte from the provided buffer
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static (int, byte) Pop(byte[] buffer, int offset)
    {
        (int next, buffer) = Slice(buffer, offset, 1);
        return (next, buffer[0]);
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
    public static (int, T) Slice<T>(Span<byte> buffer, int offset, int count, Func<byte[], T> converter)
    {
        (int next, buffer) = Slice(buffer, offset, count);
        return (next, converter(buffer.ToArray()));
    }

    /// <summary>
    ///     Slices the provided byffer
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static (int, byte[]) Slice(Span<byte> buffer, int offset, int length)
    {
        int next = offset + length;
        if (next >= buffer.Length) {
            next = 0;
            length = buffer.Length - offset;
        }
        return (next, buffer.Slice(offset, length).ToArray());
    }

    /// <summary>
    ///     Expands or contracts the provided array, 
    ///     filling empty elements with the provided fill value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="length"></param>
    /// <param name="items"></param>
    /// <param name="fill"></param>
    /// <returns></returns>
    public static Span<T> SetLength<T>(int length, Span<T> items, T fill)
    {
        if (items.Length == length) return items;
       
        var result = new Span<T>(new T[length]);
        length = length > items.Length ? items.Length : length;
        items[..length].CopyTo(result);
        result[length..].Fill(fill);
        return result;
    }

    /// <summary>
    ///     Concatenates any number of collections of bytes into a
    ///     single collection of bytes in collection and byte order.
    /// </summary>
    /// <param name="buffers"></param>
    /// <returns></returns>
    public static IEnumerable<byte> Concat(params IEnumerable<byte>[] buffers)
    {
        foreach (var buffer in buffers)
            foreach (var b in buffer)
                yield return b;
    }
}