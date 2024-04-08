using Gry;

namespace NHACP.Messages.V1;
public record NHACPV1Error : NHACPResponse
{
    public const byte TypeId = 0x82;
    public ushort Code => Bytes.ToUShort(Body[0..2]);
    public string? Message => SizedASCII(2);

    public NHACPV1Error(NHACPResponse response) : base(response)
    {
        ThrowIfBadType(TypeId);
    }

    public NHACPV1Error(ushort code, string message)
    {
        Type = TypeId;
        Body = new([
            ..Bytes.FromUShort(code),
            ..Bytes.ToSizedASCII(message).Span
        ]);
    }
}