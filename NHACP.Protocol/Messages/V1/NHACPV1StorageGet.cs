using Gry;
using System.Data;

namespace NHACP.Messages.V1;

public record NHACPV1StorageGet : NHACPRequest
{
    public const byte TypeId = 0x02;

    public byte Descriptor => Body.Span[0];
    public uint Offset => Bytes.ToUInt(Body[1..5]);
    public ushort Length => Bytes.ToUShort(Body[5..7]);

    public NHACPV1StorageGet(byte sessionId, byte descriptor, uint offset, ushort length)
    {
        SessionId = sessionId;
        Type = TypeId;
        ThrowIf<InvalidDataException>(
            () => length > NHACPConstants.MaxDataSize,
            new($"Cannot GET more than {NHACPConstants.MaxDataSize} bytes")
        );
        Body = new([
            descriptor,
            ..Bytes.FromUInt(offset),
            ..Bytes.FromUShort(length)
        ]);
    }

    public NHACPV1StorageGet(NHACPRequest request) : base(request)
    {
        ThrowIfBadType(TypeId);

        ThrowIf<InvalidDataException>(
            () => Length > NHACPConstants.MaxDataSize,
            new($"Cannot GET more than {NHACPConstants.MaxDataSize}")
        );
    }

}
