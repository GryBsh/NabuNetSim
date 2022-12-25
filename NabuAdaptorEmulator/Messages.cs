namespace Nabu;

internal static class Messages
{
    /*
     * I've tried to give distrinct messages some kind of name
     * Not guaranteed to be accurate
     */

    public const byte Reset          = 0x81;
    public const byte GetStatus    = 0x82;
    public const byte SetStatus    = 0x83;
    public const byte PacketRequest = 0x84;
    public const byte ChangeChannel = 0x85;
    public const byte Unauthorized  = 0x90;
    public const byte Authorized    = 0x91;
    public const byte Escape        = 0x10;
    public const byte ChannelStatus = 0x01;
    public const byte Ready         = 0x05;
    public const byte Good          = 0x06;
    public const byte Confirmed     = 0xE4;
    public const byte DONE          = 0xE1;

    public const byte Done = 0x1E;

    public static readonly byte[] RequestChannelCode = { 0x9F, Escape, DONE };
    public static readonly byte[] ConfirmChannelCode = { 0x1F, Escape, DONE };
    public static readonly byte[] ACK = { Escape, Good };
    public static readonly byte[] Initialized = { Escape, Good, Confirmed };
    public static readonly byte[] End = { Escape, DONE };

    public const int TimeSegment = 0x007FFFFF;
}


