using Gry;

namespace NHACP.Messages.V1;

public record NHACPV1StorageLoaded : NHACPResponse
{
    public const byte TypeId = 0x83;
    public byte Descriptor => Body.Span[0];
    public uint Size => Bytes.ToUInt(Body[1..5]);

    public NHACPV1StorageLoaded(byte descriptor, uint size)
    {
        Type = TypeId;
        Body = new([
            descriptor,
            ..Bytes.FromUInt(size)
        ]);
    }

    public NHACPV1StorageLoaded(NHACPResponse response) : base(response)
    {
        ThrowIfBadType(TypeId);
    }
}
