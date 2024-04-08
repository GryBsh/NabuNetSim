using Gry;
using Gry.Protocols;

namespace NHACP.Messages;

public record NHACPRequest : AdapterMessage
{
    public byte SessionId { get; protected init; }
    public NHACPRequest() { }
    public NHACPRequest(byte sessionId, byte type, Memory<byte>? data = null)
    {
        SessionId = sessionId;
        Type = type;
        Body = data ?? new();
    }
    public NHACPRequest(byte sessionId, Memory<byte> data) : base(data)
    {
        SessionId = sessionId;
    }

}
