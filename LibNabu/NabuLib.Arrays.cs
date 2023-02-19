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
    ///     Expands or contracts the provided array, 
    ///     filling empty elements with the result of the
    ///     provided Func[<typeparamref name="T"/>].
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="length"></param>
    /// <param name="items"></param>
    /// <param name="fill"></param>
    /// <returns></returns>
    public static Span<T> SetLength<T>(int length, Span<T> items, Func<T> fill)
    {
        if (items.Length == length) return items;

        var result = new Span<T>(new T[length]);
        var count = items.Length;
        items[..count].CopyTo(result);
        for (int i = count; i < length; i++)
        {
            result[i] = fill();
        }
        return result;
    }

    /// <summary>
    ///     Concatenates any number of collections of bytes into a
    ///     single collection of bytes in collection and byte order.
    /// </summary>
    /// <param name="buffers"></param>
    /// <returns></returns>
    public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] buffers)
    {
        foreach (var buffer in buffers)
            foreach (var b in buffer)
                yield return b;
    }

    public static Span<byte> Append(Span<byte> buffer, Span<byte> data)
    {
        Span<byte> r = new byte[buffer.Length + data.Length];
        buffer.CopyTo(r);
        data.CopyTo(r[buffer.Length..(buffer.Length+data.Length)]);
        return r;
    }

    public static Span<byte> Insert(Span<byte> buffer, int offset, Span<byte> data)
    {
        var end = offset + data.Length;
        var length = end > data.Length ? end : data.Length;
        Span<byte> r = new byte[length];
        buffer[..offset].CopyTo(r);
        data.CopyTo(r[offset..(offset + data.Length)]);
        buffer[offset..].CopyTo(r[end..]);
        return r;
    }

    public static Span<byte> Delete(Span<byte> buffer, int offset, int length)
    {
        var end = offset + length;   
        Span<byte> r = new byte[length];
        buffer[..offset].CopyTo(r);
        buffer[end..].CopyTo(r[offset..]);
        return r;
    }

    public static Span<byte> Replace(Span<byte> buffer, int offset, Span<byte> data)
    {
        var size = offset + data.Length;
        var length = size > buffer.Length ? size : buffer.Length;
        var r = SetLength<byte>(length, buffer, 0x00);

        data.CopyTo(r[offset..(offset+data.Length)]);   
        return r;
    }
}