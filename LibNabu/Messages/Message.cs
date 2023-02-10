namespace Nabu.Messages;

public static class Message
{
    /*
     * I've tried to give distinct messages some kind of name
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



