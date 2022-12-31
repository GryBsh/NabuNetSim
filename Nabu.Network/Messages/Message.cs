namespace Nabu.Messages;

public static class Message
{
    /*
     * I've tried to give distrinct messages some kind of name
     * Not guaranteed to be accurate, likely to change
     */
    public const byte Reset = 0x80;
    public const byte MagicalMysteryMessage = 0x81;
    public const byte GetStatus = 0x82;
    public const byte StartUp = 0x83;
    public const byte PacketRequest = 0x84;
    public const byte ChangeChannel = 0x85;

    public const byte Escape = 0x10;

    public static readonly byte[] ACK = { Escape, StatusMessage.Good };

    public static readonly byte[] Finished = { Escape, StateMessage.Done };

    public const int TimePak = 0x007FFFFF;
}


public static class RetroNetMessage
{
    public const byte RequestStoreHttpGet = 0xA3;
    public const byte RequestStoreGetSize = 0xA4;
    public const byte RequestStoreGetData = 0xA5;
    public const byte Telnet = 0xA6;
}

public static class SupportedProtocols
{
    public const byte NabuNet = 0x83;
    public const byte ACP = 0xAF;
}