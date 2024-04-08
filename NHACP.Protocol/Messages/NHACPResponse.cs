using Gry;
using Gry.Protocols;
using NHACP.Messages.V1;

namespace NHACP.Messages;

public record NHACPResponse : AdapterMessage
{
    public bool IsError => Type is NHACPV1Error.TypeId;
    public NHACPResponse() { }
    public NHACPResponse(Memory<byte> data) : base(data) { }

    public static implicit operator Memory<byte>(NHACPResponse message)
    {
        return new([
            message.Type,
            ..message.Body.Span
        ]);
    }
}
