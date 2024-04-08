namespace Nabu.Protocols;

public record CtrlRequest(CtrlItemType Type, CtrlCommand Command, Memory<byte>? Data)
{
    public static CtrlRequest FromBuffer(Memory<byte> data)
    {
        return new(
            (CtrlItemType)data.Span[0],
            (CtrlCommand)data.Span[1],
            data.Length > 2 ? data[2..] : null
        );
    }

    public static Memory<byte> ToBuffer(CtrlRequest request)
    {
        return new([
            (byte)request.Type,
            (byte)request.Command,
            ..request.Data is not null ? request.Data.Value.Span : []
        ]);
    }

}
