using Gry;
using System.Text;

namespace Nabu.Protocols;

public record CtrlItem(CtrlItemType Type, CtrlValueType ValueType, string Label, Memory<byte>? Value)
{
    public static CtrlItem Error(byte code, string message) => new(CtrlItemType.Error, CtrlValueType.Byte, message, new([code]));

    public static CtrlItem FromBuffer(Memory<byte> data)
    {
        int i = 0;
        var type = (CtrlItemType)data.Span[i++];
        var valueType = (CtrlValueType)data.Span[i++];
        var labelLength = data.Span[i++];
        var label = Encoding.ASCII.GetString(data.Span[i..labelLength]);
        var value = valueType > 0 ? data[(i + label.Length)..] : null;
        return new(type, valueType, label, value);
    }

    public static Memory<byte> ToBuffer(CtrlItem item)
    {
        var label = Encoding.ASCII.GetBytes(item.Label);
        return new([
            (byte)item.Type,
            (byte)item.ValueType,
            (byte)label.Length,
            ..label,
            ..item.ValueType > 0 ? item.Value is not null ? [(byte)item.Value.Value.Length] : [(byte)0] : Array.Empty<byte>(),
            ..item.ValueType > 0 && item.Value is not null ? item.Value.Value.Span : []
        ]);
    }
    

    public static CtrlValueType TypeOf(Type type) => type switch
    {
        Type t when t == typeof(int) => CtrlValueType.Int,
        Type t when t == typeof(uint) => CtrlValueType.UInt,
        Type t when t == typeof(short) => CtrlValueType.Short,
        Type t when t == typeof(ushort) => CtrlValueType.UShort,
        Type t when t == typeof(byte) => CtrlValueType.Byte,
        Type t when t == typeof(string) => CtrlValueType.String,
        Type t when t == typeof(byte[]) => CtrlValueType.Array,
        _ => CtrlValueType.None
    };

    public static Memory<byte>? ValueOf(object? value) 
        => value is null ? null : ValueOf(TypeOf(value.GetType()), value);

    public static Memory<byte>? ValueOf(CtrlValueType type, object? value)
    {
        if (value is null) return null;
        return type switch
        {
            CtrlValueType.Int or CtrlValueType.UInt => (Memory<byte>?)NabuLib.FromInt((int)value),
            CtrlValueType.Short or CtrlValueType.UShort => (Memory<byte>?)NabuLib.FromUShort((ushort)value),
            CtrlValueType.Byte => new([(byte)value]),
            CtrlValueType.String => (Memory<byte>?)ControlProtocol.Text((string)value),
            CtrlValueType.Array => (Memory<byte>?)(byte[])value,
            _ => null,
        };
    }

    public static CtrlItem FromValue(CtrlItemType Type, string Label, object? value)
    {
        if (value is null) return new(Type, CtrlValueType.None, Label, null);

        var type = TypeOf(value.GetType());
        return new(Type, type, Label, ValueOf(type, value));
    }

    public static object? GetValue(Type type, Memory<byte> data) => GetValue(TypeOf(type), data);

    public static object? GetValue(CtrlValueType type, Memory<byte> data)
    {
        return type switch
        {
            CtrlValueType.None => null,
            CtrlValueType.Byte => data.Span[0],
            CtrlValueType.Short => (short)Bytes.ToUShort(data),
            CtrlValueType.UShort => Bytes.ToUShort(data),
            CtrlValueType.Int => Bytes.ToInt(data),
            CtrlValueType.UInt => (uint)Bytes.ToInt(data),
            CtrlValueType.String => Encoding.ASCII.GetString(data.Span),
            CtrlValueType.Array => data.ToArray(),
            _ => null
        };
    }

    public static object? GetValue(CtrlItem item)
    {
        if (item.Value is null) return null;

        return GetValue(item.ValueType, item.Value.Value);
        
    }
}
