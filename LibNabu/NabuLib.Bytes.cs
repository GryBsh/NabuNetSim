using System.Text;

namespace Nabu;

public static partial class NabuLib
{
    /// <summary>
    ///     Converts s little-endian 32 bit integer
    ///     in bytes to Int
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static int ToInt(Memory<byte> buffer)
    {
        var span = SetLength<byte>(4, buffer, 0x00).Span;
        int r = 0;
        r |= span[0] << 0;
        r |= span[1] << 8;
        r |= span[2] << 16;
        r |= span[3] << 24;
        return r;
    }

    /// <summary>
    ///     Converts an Int to a little-endian
    ///     32 bit integer in bytes
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static byte[] FromInt(int number)
    {
        var buffer = new byte[4];
        buffer[0] = (byte)(number >> 0 & 0xFF);
        buffer[1] = (byte)(number >> 8 & 0xFF);
        buffer[2] = (byte)(number >> 16 & 0xFF);
        buffer[3] = (byte)(number >> 24 & 0xFF);
        return buffer;
    }

    /// <summary>
    ///     Converts a little-endian 16 bit integer
    ///     in bytes to Short
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static short ToShort(Memory<byte> buffer)
    {
        var span = SetLength<byte>(2, buffer, 0x00).Span;
        int r = 0;
        r |= span[0] << 0;
        r |= span[1] << 8;
        return (short)r;
    }

    /// <summary>
    ///     Converts an Short to a little-endian
    ///     16 bit integer in bytes
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static byte[] FromShort(short number)
    {
        var buffer = new byte[2];
        buffer[0] = (byte)(number >> 0 & 0xFF);
        buffer[1] = (byte)(number >> 8 & 0xFF);
        return buffer;
    }

    /// <summary>
    ///     Converts a String to ASCII bytes,
    ///     prefixed by 1 byte for it's size
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Memory<byte> ToSizedASCII(string str, int length = 0)
    {
        var bytes = Encoding.ASCII.GetBytes(str);

        if (length is 0)
            length = bytes.Length;

        var r = new Memory<byte>(new byte[length + 1]);
        r.Span[0] = (byte)length;
        if (bytes.Length > length)
            bytes[..length].CopyTo(r[1..]);
        else
            bytes.CopyTo(r[1..]);

        return r;

        /*
        yield return (byte)str.Length;
        foreach (int index in Enumerable.Range(0, length))
            if (index >= bytes.Length) yield return 0x00;
            else yield return bytes[index];
        */
    }

    /// <summary>
    ///     Converts ASCII bytes to a String
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static string ToASCII(Memory<byte> buffer)
        => Encoding.ASCII.GetString(buffer.ToArray());

    // <summary>
    ///     Converts  a String
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static Memory<byte> FromASCII(string buffer)
        => Encoding.ASCII.GetBytes(buffer);

    /// <summary>
    ///     Converts a Boolean into a byte
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static byte FromBool(bool value) => (byte)(value ? 0x01 : 0x00);

    /// <summary>
    ///     Creates a String in X02 format from the given byte
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public static string Format(byte b) => $"{b:X02}";

    /// <summary>
    ///     Creates a String in X06 format from the given Int
    ///     (3 hextets)
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string FormatTriple(int bytes) => $"{bytes:X06}";

    /// <summary>
    ///     Creates a seperated String of bytes in X02 format
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string FormatSeperated(params byte[] bytes)
    {
        var parts = bytes.Select(b => Format(b)).ToArray();
        return string.Join('|', parts);
    }

    /// <summary>
    ///     Creates a String of bytes in X02 format.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string Format(params byte[] bytes)
    {
        var parts = bytes.Select(b => Format(b)).ToArray();
        return string.Join(string.Empty, parts);
    }
}