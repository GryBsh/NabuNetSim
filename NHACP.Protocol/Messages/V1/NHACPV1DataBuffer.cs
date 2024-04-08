using Gry;

namespace NHACP.Messages.V1;

public record NHACPV1DataBuffer : NHACPResponse
{
    public const byte TypeId = 0x84;

    public ushort Length => Bytes.ToUShort(Body[0..2]);
    public Memory<byte> Data => Body[2..];

    public NHACPV1DataBuffer(byte descriptor, Memory<byte> buffer)
    {
        Type = TypeId;
        Body = new([
            ..Bytes.FromUShort((ushort)buffer.Length),
            ..buffer.Span
        ]);
    }

    public NHACPV1DataBuffer(NHACPResponse message) : base(message)
    {
        ThrowIfBadType(TypeId);
    }
}
