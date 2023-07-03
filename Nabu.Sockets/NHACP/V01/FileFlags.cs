namespace Nabu.Network.NHACP.V01;

[Flags]
public enum AttrFlags : short
{
    None = 0x0000,
    Read = 0x0001,
    Write = 0x0002,
    Directory = 0x0004,
    Special = 0x0008
}

[Flags]
public enum OpenFlags
{
    ReadOnly = 0x0000,
    ReadWrite = 0x0001,
    ReadWriteProtect = 0x0002,
    Directory = 0x0008,
    Create = 0x0010,
    Exclusive = 0x0020,
    Truncate = 0x0040
}

[Flags]
public enum RemoveFlags
{
    RemoveFile = 0,
    RemoveDir = 1
}