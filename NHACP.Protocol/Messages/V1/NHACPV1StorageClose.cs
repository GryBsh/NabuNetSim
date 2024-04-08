namespace NHACP.Messages.V1;public record NHACPV1StorageClose : NHACPRequest
{
    public const byte TypeId = 0x05;
    public byte Descriptor => Body.Span[0];
    

    public NHACPV1StorageClose(byte sessionId, byte descriptor)
    {
        SessionId = sessionId;
        Type = TypeId;
        Body = new([
            descriptor,
        ]);
    }

    public NHACPV1StorageClose(NHACPRequest request) : base(request)
    {
        ThrowIfBadType(TypeId);
    }
}
