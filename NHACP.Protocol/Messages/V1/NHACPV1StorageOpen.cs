using Gry;
using NHACP.V01;
using System;

namespace NHACP.Messages.V1;
public enum NHACPV1RequestType : byte{    Hello = 0x00,    StorageOpen = 0x01,    StorageGet = 0x02,    StorageClose = 0x05,}
public record NHACPV1StorageOpen : NHACPRequest
{
    public const byte TypeId = 0x01;
    public byte Descriptor => Body.Span[0];
    public NHACPOpenFlags Flags => (NHACPOpenFlags)Bytes.ToUShort(Body[1..3]);
    public string? Url => SizedASCII(3);

    public NHACPV1StorageOpen(byte sessionId, byte descriptor, NHACPOpenFlags flags, string url)
    {
        SessionId = sessionId;
        Type = TypeId;
        Body = new([
            descriptor,
            ..Bytes.FromUShort((ushort)flags),
            ..Bytes.ToSizedASCII(url).Span,
        ]);
    }

    public NHACPV1StorageOpen(NHACPRequest request) : base(request)
    {
        ThrowIfBadType(TypeId);
    }
}