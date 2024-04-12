using Gry.Adapters;

namespace Nabu.Protocols.RetroNet;

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
    public const byte FileHandleSeek = 0xB6;    public const byte RetroNetHeadless = 0xBA;
    public const byte FileHandleLineCount = 0xDC;
    public const byte FileHandleGetLine = 0xDD;

    public const byte GetParentCount = 0xBA;
    public const byte GetParentName = 0xBB;
    public const byte GetChildCount = 0xBC;
    public const byte GetChildName = 0xBD;
    public const byte SetSelection = 0xBE;

    public const byte TCPHandleOpen = 0xD0;
    public const byte TCPHandleClose = 0xD1;
    public const byte TCPHandleSize = 0xD2;
    public const byte TCPHandleRead = 0xD3;
    public const byte TCPHandleWrite = 0xD4;

    public const byte TCPServerClientCount = 0xD5;
    public const byte TCPServerAvailable = 0xD6;
    public const byte TCPServerRead = 0xD7;
    public const byte TCPServerWrite = 0xD8;

    public const byte Printer = 0xDA;
    public const byte PunchOut = 0xDB;

    public const byte BuildDrive = 0xDE;
    public const byte ExpandDrive = 0xDF;
}public static class RetroNetHeadless {         public const byte GetParentCount = 0x00;    public const byte GetParentName = 0x01;    public const byte GetParentDescription = 0x0D;    public const byte GetChildCount = 0x02;    public const byte GetChildName = 0x03;    public const byte SetSelection = 0x04;    public const byte GetChildDescription = 0x05;    public const byte GetChildAuthor = 0x06;    public const byte GetNewsContent = 0x07;    public const byte GetChildIconTileColor = 0x08;    public const byte GetChildIconTilePattern = 0x09;    public const byte GetLog = 0x0A;    public const byte GetNewsTitle = 0x0B;    public const byte GetNewsDate = 0x0C;    public const byte GetNewsCount = 0x0E;    public const byte GetNewsContentById = 0x0F;    public const byte GetNewsTitleById = 0x10;    public const byte GetNewsDateById = 0x11;    public const byte GetNewsIconTileColor = 0x12;    public const byte GetNewsIconTilePattern = 0x13;    public const byte GetOperatingSystem = 0x14;}  