using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gry;

public static partial class Bytes
{
    /// <summary>
    ///    Appends the provided data to the provided buffer
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Memory<byte> Append(Memory<byte> buffer, Memory<byte> data)
    {
        Memory<byte> r = new byte[buffer.Length + data.Length];
        buffer.CopyTo(r);
        data.CopyTo(r[buffer.Length..(buffer.Length + data.Length)]);
        return r;
    }

    /// <summary>
    ///     Concatenates any number of arrays of bytes into a
    ///     single collection of bytes in byte order from the first byte
    ///     of the first array to the last byte of the last array.
    /// </summary>
    /// <param name="buffers"></param>
    /// <returns></returns>
    public static Memory<T> Concat<T>(params Memory<T>[] buffers)
    {
        return buffers.SelectMany(x => x.ToArray()).ToArray();
    }

    public static Memory<byte> Delete(Memory<byte> buffer, int offset, int length)
    {
        var end = offset + length;
        Memory<byte> r = new byte[length];
        buffer[..offset].CopyTo(r);
        buffer[end..].CopyTo(r[offset..]);
        return r;
    }

    public static Memory<byte> Insert(Memory<byte> buffer, int offset, Memory<byte> data)
    {
        var end = (buffer.Length - offset) + data.Length;
        var length = end > buffer.Length ? end : buffer.Length;
        Memory<byte> r = new byte[length];
        buffer[..offset].CopyTo(r);
        data.CopyTo(r[offset..(offset + data.Length)]);
        buffer[offset..].CopyTo(r[end..]);
        return r;
    }

    /// <summary>
    ///    Replaces the bytes at the provided offset with the provided data
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Memory<byte> Replace(Memory<byte> buffer, int offset, Memory<byte> data)
    {
        var size = offset + data.Length;
        var length = size > buffer.Length ? size : buffer.Length;
        var r = SetLength<byte>(length, buffer, 0x00);

        data.CopyTo(r[offset..(offset + data.Length)]);
        return r;
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
    public static Memory<T> SetLength<T>(int length, Memory<T> items, T? fill = default)
    {
        if (items.Length == length) return items;

        var result = new Memory<T>(new T[length]);
        length = length > items.Length ? items.Length : length;
        items[..length].CopyTo(result);
        if (fill is not null)
            result[length..].Span.Fill(fill);
        return result;
    }

    /// <summary>
    ///     Makes a memory of bytes from integers and integer literals
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static Memory<byte> Array(params int[] values)
    {
        var buffer = new Memory<byte>(new byte[values.Length]);
        for (int i = 0; i < values.Length; i++)
            buffer.Span[i] = (byte)values[i];
        return buffer;
    }

    
}
