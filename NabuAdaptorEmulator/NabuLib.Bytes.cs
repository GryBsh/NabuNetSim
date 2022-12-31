using System.Text;

namespace Nabu;

public static partial class NabuLib
{
    public static int ToInt(params byte[] buffer)
    {
        buffer = SetLength<byte>(4, buffer, 0x00);
        int r = 0;
        r |= buffer[0] << 0;
        r |= buffer[1] << 8;
        r |= buffer[2] << 16;
        r |= buffer[3] << 24;
        return r;
    }

    public static byte[] FromInt(int number)
    {
        var buffer = new byte[4];
        buffer[0] = (byte)(number >> 0 & 0xFF);
        buffer[1] = (byte)(number >> 8 & 0xFF);
        buffer[2] = (byte)(number >> 16 & 0xFF);
        buffer[3] = (byte)(number >> 24 & 0xFF);
        return buffer;
    }

    public static short ToShort(params byte[] buffer)
    {
        buffer = SetLength<byte>(2, buffer, 0x00);
        int r = 0;
        r |= buffer[0] << 0;
        r |= buffer[1] << 8;
        return (short)r;
    }

    public static byte[] FromShort(short number)
    {
        var buffer = new byte[2];
        buffer[0] = (byte)(number >> 0 & 0xFF);
        buffer[1] = (byte)(number >> 8 & 0xFF);
        return buffer;
    }

    public static IEnumerable<byte> FromASCII(string str)
    {
        yield return (byte)str.Length;
        foreach (byte b in Encoding.ASCII.GetBytes(str))
            yield return b;
    }

    public static string ToASCII(byte[] buffer) 
        => Encoding.ASCII.GetString(buffer);

    public static byte FromBool(bool value) => (byte)(value ? 0x01 : 0x00);

    public static string Format(byte b) => $"{b:X02}";
    public static string FormatTriple(int bytes) => $"{bytes:X06}";
    public static string FormatSeperated(params byte[] bytes)
    {
        var parts = bytes.Select(b => Format(b)).ToArray();
        return string.Join('|', parts);
    }
    public static string Format(params byte[] bytes)
    {
        var parts = bytes.Select(b => Format(b)).ToArray();
        return string.Join(string.Empty, parts);
    }
}