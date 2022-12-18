namespace Nabu;

public static partial class Tools
{
    public static byte[] SetLength(int length, params byte[] bytes)
    {
        if (bytes.Length == length) return bytes;

        var buffer = new byte[length];
        for (int i = 0; i < length; i++)
        {
            if (bytes.Length > i)
            {
                buffer[i] = bytes[i];
            }
            else
            {
                buffer[i] = 0x00;
            }
        }
        return buffer;
    }
}
