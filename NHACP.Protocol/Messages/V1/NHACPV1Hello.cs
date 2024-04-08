using Gry;
using NHACP.V01;
using System.Data;

namespace NHACP.Messages.V1;

public record NHACPV1Hello : NHACPRequest
{
    public const byte TypeId = 0x00;
    public const ushort SupportedVersion = 0x0001;
    public const string ProperMagic = "ACP";

    public readonly byte[] ValidSessionIds = [
        (byte)NHACPV1SessionId.System,
        (byte)NHACPV1SessionId.User
    ];
    public string Magic => Bytes.ToASCII(Body[0..3]);
    public ushort Version => Bytes.ToUShort(Body[3..5]);
    public NHACPOptions Flags => (NHACPOptions)Bytes.ToUShort(Body[5..7]);
    public NHACPV1Hello(
        NHACPOptions flags, 
        ushort version = 1, 
        NHACPV1SessionId sessionId = NHACPV1SessionId.User
    )
    {
        SessionId = (byte)sessionId;
        Type = TypeId;
        Body = new([
            ..Bytes.FromASCII(ProperMagic).Span,
            ..Bytes.FromUShort(version),
            ..Bytes.FromUShort((ushort)flags)
        ]);
    }
    public NHACPV1Hello(NHACPRequest request) : base(request)
    {
        ThrowIfBadType(TypeId);

        ThrowIf<InvalidDataException>(
            () => !ValidSessionIds.Contains(SessionId),
            new($"SessionId `{SessionId}` is not in `{Bytes.FormatSeparated(',', ValidSessionIds)}`")
        );

        ThrowIf<InvalidDataException>(
            () => Magic != ProperMagic,
            new($"Magic string `{Magic}` does not match `{ProperMagic}`")
        );
    }
}
