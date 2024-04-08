using Gry;

namespace NHACP.Messages.V1;

public record NHACPV1DateTime : NHACPResponse
{
    public const byte TypeId = 0x85;
    public string? Date => FromASCII(0, 8);
    public string? Time => FromASCII(8, 6);
    public NHACPV1DateTime(NHACPResponse message) : base(message)
    {
        ThrowIfBadType(TypeId);
    }

    public NHACPV1DateTime(DateTime date)
    {
        Type = TypeId;
        Body = DateTime(date);
    }
    public NHACPV1DateTime(Memory<byte> buffer) : base(buffer) { }

    Memory<byte> DateTime(DateTime date)
    {
        return new([
            ..Bytes.FromASCII(date.ToString("yyyyMMdd")).Span,
            ..Bytes.FromASCII(date.ToString("HHmmss")).Span
        ]);
    }

}

