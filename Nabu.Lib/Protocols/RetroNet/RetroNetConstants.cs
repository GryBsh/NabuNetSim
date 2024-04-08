using Gry;
using Gry.Adapters;
using Gry.Protocols;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Settings;
using Nabu.Sources;
using System.Text;
using System.Text.RegularExpressions;

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
}public static class RetroNetHeadless {         public const byte GetParentCount = 0x00;    public const byte GetParentName = 0x01;    public const byte GetParentDescription = 0x0D;    public const byte GetChildCount = 0x02;    public const byte GetChildName = 0x03;    public const byte SetSelection = 0x04;    public const byte GetChildDescription = 0x05;    public const byte GetChildAuthor = 0x06;    public const byte GetNewsContent = 0x07;    public const byte GetChildIconTileColor = 0x08;    public const byte GetChildIconTilePattern = 0x09;    public const byte GetLog = 0x0A;    public const byte GetNewsTitle = 0x0B;    public const byte GetNewsDate = 0x0C;    public const byte GetNewsCount = 0x0E;    public const byte GetNewsContentById = 0x0F;    public const byte GetNewsTitleById = 0x10;    public const byte GetNewsDateById = 0x11;    public const byte GetNewsIconTileColor = 0x12;    public const byte GetNewsIconTilePattern = 0x13;    public const byte GetOperatingSystem = 0x14;}  public partial class RetroNetHeadlessProtocol(    ILogger<RetroNetHeadlessProtocol> logger,    ISourceService sources,
    INabuNetwork network,
    GlobalSettings globalSettings) : Protocol<AdaptorSettings>(logger){    public override byte[] Messages { get; } = [       RetroNetCommands.RetroNetHeadless    ];    public override byte Version => 0x01;    const string Empty = " ";    #region Retronet Headless Ops    byte[] GetString(string message, int? count = null)    {        count ??= message.Length;        return [            (byte)count,            ..Bytes.SetLength(count.Value, Bytes.FromASCII(message)).ToArray()        ];    }    void GetParentName()    {        var item = GetSource();        Write(GetString(item?.Name ?? Empty));    }    void GetParentDescription()    {        var id = Read();        var item = sources.List.ElementAtOrDefault(id);        Write(            GetString(item?.Description ?? Empty, 64)        );    }    void GetChildCount()    {        var id = Read();        var item = sources.List.ElementAtOrDefault(id);        if (item is null) Write(0x00);        Write((byte)ProgramPage(network.Programs(item)).Count());    }    void GetChildName()    {        var parent = Read();        var child = Read();        var (parentItem, childItem) = GetItem(parent, child);        if (parentItem is null || childItem is null) Write(0x00);        else Write(            GetString(childItem.DisplayName ?? Empty)        );    }    void SetSelection()    {        var parent = Read();        var child = Read();                       var (parentItem, childItem) = GetItem(parent, child);        if (parentItem is null || childItem is null) return;        SetReturn();        Adapter!.Source = parentItem.Name;        Adapter!.Program = childItem.Name;    }    private void SetReturn()
    {
        Adapter!.ReturnToSource = Adapter!.Source;
        Adapter!.ReturnToProgram = Adapter!.Program;
    }    void GetChildDescription()    {        var parent = Read();        var child = Read();        var (parentItem, childItem) = GetItem(parent, child);        if (parentItem is null || childItem is null) Write(0x00);        else Write(            GetString(childItem.Description ?? Empty, 64)        );    }    void GetChildAuthor()    {        var parent = Read();        var child = Read();        var (parentItem, childItem) = GetItem(parent, child);        if (parentItem is null || childItem is null) Write(0x00);        else Write(            GetString(childItem.Author ?? Empty)        );    }    void GetChildIconTileColor()    {        var parent = Read();        var child = Read();        var (parentItem, childItem) = GetItem(parent, child);        if (parentItem is null || childItem is null) Write(0x00);        else Write(            Convert.FromBase64String(childItem.TileColor)            );    }    void GetChildIconTilePattern()    {        var parent = Read();        var child = Read();        var (parentItem, childItem) = GetItem(parent, child);        if (parentItem is null || childItem is null) Write(0x00);        else Write(            Convert.FromBase64String(childItem.TilePattern)        );    }    const string message = "Welcome to NABU NetSim!";    const string longMessage = "It works for us now comrade.";    static byte[] MessageBytes { get; } = Encoding.ASCII.GetBytes(message);    static byte[] LongMessageBytes { get; } = Encoding.ASCII.GetBytes(longMessage);    byte[] LongBinaryMessage { get; } = [..Bytes.FromUShort((ushort)LongMessageBytes.Length), ..LongMessageBytes];    byte[] BinaryMessage { get; } = Bytes.ToSizedASCII(message).ToArray();    byte[] Date { get; } = Bytes.ToSizedASCII(DateTime.Now.ToString("MMMM dd, yyyy")).ToArray();    #endregion    protected override Task Handle(byte unhandled, CancellationToken cancel)    {        var command = Read();        switch (command)        {            case RetroNetHeadless.GetParentCount:                Logger.LogInformation("GetParentCount");                Write((byte)sources.List.Count());                break;            case RetroNetHeadless.GetParentName:                Logger.LogInformation(nameof(GetParentName));                GetParentName();                break;            case RetroNetHeadless.GetParentDescription:                Logger.LogInformation(nameof(GetParentDescription));                GetParentDescription();                break;            case RetroNetHeadless.GetChildCount:                Logger.LogInformation(nameof(GetChildCount));                GetChildCount();                break;            case RetroNetHeadless.GetChildName:                Logger.LogInformation(nameof(GetChildName));                GetChildName();                break;            case RetroNetHeadless.SetSelection:                Logger.LogInformation(nameof(SetSelection));                SetSelection();                break;            case RetroNetHeadless.GetChildDescription:                Logger.LogInformation(nameof(GetChildDescription));                GetChildDescription();                break;            case RetroNetHeadless.GetChildAuthor:                Logger.LogInformation(nameof(GetChildAuthor));                GetChildAuthor();                break;            case RetroNetHeadless.GetNewsContent:                Logger.LogInformation("GetNewsContent");                Write(LongBinaryMessage);                break;            case RetroNetHeadless.GetChildIconTileColor:                Logger.LogInformation(nameof(GetChildIconTileColor));                GetChildIconTileColor();                break;            case RetroNetHeadless.GetChildIconTilePattern:                Logger.LogInformation(nameof(GetChildIconTilePattern));                GetChildIconTilePattern();                break;            case RetroNetHeadless.GetLog:                Logger.LogInformation("GetLog");                Write(LongBinaryMessage);                break;            case RetroNetHeadless.GetNewsTitle:                Logger.LogInformation("GetNewsTitle");                Write(BinaryMessage);                break;            case RetroNetHeadless.GetNewsDate:                Logger.LogInformation("GetNewsDate");                Write(Date);                break;            case RetroNetHeadless.GetNewsCount:                Logger.LogInformation("GetNewsCount");                Write(1);                break;            case RetroNetHeadless.GetNewsContentById:                Read();                Logger.LogInformation("GetNewsContentById");                Write(LongBinaryMessage);                break;            case RetroNetHeadless.GetNewsTitleById:                Read();                Logger.LogInformation("GetNewsTitleById");                Write(BinaryMessage);                break;            case RetroNetHeadless.GetNewsDateById:                Read();                Logger.LogInformation("GetNewsDateById");                Write(Date);                break;            case RetroNetHeadless.GetNewsIconTileColor:                Read();                Logger.LogInformation("GetNewsIconTileColor");                Write(Convert.FromBase64String(NabuNetwork.BlankIconClrStr));                break;            case RetroNetHeadless.GetNewsIconTilePattern:                Read();                Logger.LogInformation("GetNewsIconTilePattern");                Write(Convert.FromBase64String(NabuNetwork.BlankIconPtrnStr));                break;            case RetroNetHeadless.GetOperatingSystem:                Logger.LogInformation("GetOperatingSystem");                byte os = 0 switch                {                    _ when OperatingSystem.IsWindows() => 0,                    _ when OperatingSystem.IsMacOS() => 1,                    _ when OperatingSystem.IsLinux() => 2,                    _ => 99                };                Write(os);                break;        }        return Task.CompletedTask;    }        #region Helpers    bool IsNotPakFile(NabuProgram? program)             => program is not null &&                !NabuLib.IsPakFile(program.Name);    IEnumerable<NabuProgram> ProgramPage(IEnumerable<NabuProgram> programs)        => programs.Where(IsNotPakFile);    ProgramSource? GetSource()    {        var id = Read();        return sources.List.ElementAtOrDefault(id);    }    (ProgramSource?, NabuProgram?) GetItem(byte parent, byte child)    {        var parentItem = sources.List.ElementAtOrDefault(parent);                return (            parentItem,            network.Programs(parentItem).ElementAtOrDefault(child)        );    }    #endregion    [GeneratedRegex("[^\\u0000-\\u007F]+")]    private static partial Regex ASCII();}