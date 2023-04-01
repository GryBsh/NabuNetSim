namespace Nabu.Network.NHACP.V01;

public static class NHACPMessage
{
    public const string Magic = "ACP";
    public static short[] SupportedVersions = new short[] { 0, 1 };



    public static Span<byte> Frame(byte type, params IEnumerable<byte>[] message) {
        return NabuLib.Frame(
            new[] { type },
            message
        );
    }

    public static Span<byte> NHACPStarted(byte sessionId, short version, string id)
    {
        return Frame(
            0x80,
            new byte[] { sessionId },
            NabuLib.FromShort(version),
            NabuLib.ToSizedASCII("NONE")
        );
    }

    public static Span<byte> OK() => new byte[] { 0x81 };

    public static Span<byte> Error(NHACPError code, string message)
    {
        return Frame(
            0x82,
            NabuLib.FromShort((short)code),
            NabuLib.ToSizedASCII(message)
        );
    }

    public static Span<byte> StorageLoaded(byte index, int size)
    {
        return Frame(
            0x83,
            new[] { index },
            NabuLib.FromInt(size)
        );
    }

    public static Span<byte> Buffer(byte[] buffer)
    {
        return Frame(
            0x84,
            NabuLib.FromShort((short)buffer.Length),
            buffer
        );
    }

    public static Span<byte> DateTime(DateTime date)
    {
        return Frame(
            0x85,
            NHACPStructure.DateTime(date).ToArray()
        );
    }

    public static Span<byte> DirectoryEntry(string path)
    {
        return Frame(
            0x86,
            NHACPStructure.FileInfo(path).ToArray(),
            NabuLib.ToSizedASCII(Path.GetFileName(path))
        );
    }

    public static Span<byte> Byte(byte byt)
    {
        return Frame(
            0x87,
            new[] {byt} 
        );
    }

    public static Span<byte> Short(short shrt)
    {
        return Frame(
            0x88,
            NabuLib.FromShort(shrt)
        );
    }

    public static Span<byte> Int(int number)
    {
        return Frame(
            0x88,
            NabuLib.FromInt(number)
        );
    }
}
