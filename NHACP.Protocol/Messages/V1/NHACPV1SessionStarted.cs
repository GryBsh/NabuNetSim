using Gry;
using NHACP.V01;

namespace NHACP.Messages.V1;

public record NHACPV1SessionStarted : NHACPResponse
{
    public const byte TypeId = 0x80;
    public byte SessionId => Body.Span[0];
    public ushort Version => Bytes.ToUShort(Body[1..3]);
    public string? AdapterId => SizedASCII(3);
    public NHACPOptions Flags { get; init; }

    public NHACPV1SessionStarted(NHACPOptions flags, NHACPResponse message) : base(message)
    {
        Flags = flags;
    }

    public NHACPV1SessionStarted(
        byte sessionId,
        ushort version,
        string adapterId
    )
    {
        Type = TypeId;
        Body = new([
            sessionId,
            ..Bytes.FromUShort(version),
            ..Bytes.ToSizedASCII(adapterId).Span
        ]);
    }
}
