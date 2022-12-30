namespace Nabu;

public static partial class NABU
{
    public static (int, byte[]) SliceArray(byte[] buffer, int offset, int length)
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
}