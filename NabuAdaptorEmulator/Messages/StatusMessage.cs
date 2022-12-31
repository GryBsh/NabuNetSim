namespace Nabu.Messages;

public static class StatusMessage
{
    public const byte Signal = 0x01;

    public const byte Ready = 0x05;
    public const byte Good = 0x06;

    public const byte MysteryStatus = 0x1E;
}