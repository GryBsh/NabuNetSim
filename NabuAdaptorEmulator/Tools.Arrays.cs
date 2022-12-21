namespace Nabu;

public static partial class Tools
{
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
