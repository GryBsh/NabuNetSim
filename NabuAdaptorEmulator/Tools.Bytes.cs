namespace Nabu;

public static partial class Tools
{
    public static int PackInt(byte[] buffer) 
    {
        buffer = SetLength(4, buffer);
        int r = 0;
        r |= buffer[0] << 0;
        r |= buffer[1] << 8;
        r |= buffer[2] << 16;
        r |= buffer[3] << 24;
        return r;
    }

    public static short PackShort(byte[] buffer)
    {
        buffer = SetLength(2, buffer);
        int r = 0;
        r |= buffer[0] << 0;
        r |= buffer[1] << 8;
        return (short)r;
    }

    public static string Format(byte b) => $"{b:X02}";
    public static string FormatTriple(int bytes) => $"{bytes:X06}";
    public static string Format(byte[] bytes)
    {
        var parts = bytes.Select(b => Format(b)).ToArray();
        return string.Join('|', parts);
    }
}
