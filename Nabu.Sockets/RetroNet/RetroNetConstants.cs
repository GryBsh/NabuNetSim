namespace Nabu.Network.RetroNet;

public static class RetroNetCommands
{
    public const byte FileOpen = 0xA3;
    public const byte FileHandleSize = 0xA4;
    public const byte FileHandleRead = 0xA5;
    public const byte FileHandleClose = 0xA7;
    // <-- Because A6 is Telnet
    public const byte FileSize = 0xA8;
    public const byte FileHandleAppend = 0xA9;
    public const byte FileHandleInsert = 0xAA;
    public const byte FileHandleDeleteRange = 0xAB;
    public const byte FileHandleReplaceRange = 0xAC;
    public const byte FileDelete = 0xAD;
    public const byte FileCopy = 0xAE;
    public const byte FileMove = 0xAF;
    public const byte FileHandleTruncate = 0xB0;
    public const byte FileList = 0xB1;
    public const byte FileIndexStat = 0xB2;
    public const byte FileStat = 0xB3;
    public const byte FileHandleDetails = 0xB4;
    public const byte FileHandleReadSequence = 0xB5;
    public const byte FileHandleSeek = 0xB6;
}

