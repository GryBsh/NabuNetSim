namespace Nabu;

public static partial class NabuLib
{
    public static (int, byte) Pop(byte[] buffer, int offset)
    {
        (int next, buffer) = Slice(buffer, offset, 1);
        return (next, buffer[0]);
    }
    public static (int, T) Slice<T>(byte[] buffer, int offset, int count, Func<byte[], T> converter)
    {
        (int next, buffer) = Slice(buffer, offset, count);
        return (next, converter(buffer));
    }

    public static (int, byte[]) Slice(byte[] buffer, int offset, int length)
    {
        int next = offset + length;
        if (next >= buffer.Length) {
            next = 0;
            length = buffer.Length - offset;
        }
        return (next, buffer.AsSpan().Slice(offset, length).ToArray());
    }

    public static T[] SetLength<T>(int length, T[] items, T fill)
    {
        if (items.Length == length) return items;
        var result = new T[length];
        result.AsSpan().Fill(fill);
        length = length > items.Length ? items.Length : length;
        items.AsSpan()[..length].CopyTo(result);
        return result;
    }

    public static IEnumerable<byte> Concat(params IEnumerable<byte>[] buffers)
    {
        foreach (var buffer in buffers)
            foreach (var b in buffer)
                yield return b;
    }
}