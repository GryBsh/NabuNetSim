namespace Nabu;

public static partial class NabuLib
{
    public static Span<byte> Frame(byte[] header, params IEnumerable<byte>[] buffer)
    {
        return Concat(header, Concat(buffer)).ToArray().AsSpan();
    }
}