namespace Nabu;

public static partial class NABU
{
    public static (int, byte[]) SliceArray(byte[] buffer, int offset, int length)
    {
        int next = offset + length;
        
        if (next >= buffer.Length)
        {
            next = 0;
            length = buffer.Length - offset;
        }

        length = offset + length;
        return (next, buffer[offset..length]);
    }

    public static T[] SetLength<T>(int length, T[] items, T fill)
    {
        if (items.Length == length) return items;

        var result = new T[length];
        for (int i = 0; i < length; i++)
        {
            if (items.Length > i)
            {
                result[i] = items[i];
            }
            else
            {
                result[i] = fill;
            }
        }
        return result;
    }
}
