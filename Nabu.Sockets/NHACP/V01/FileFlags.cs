namespace Nabu.Network.NHACP.V01;

[Flags]
public enum AttrFlags : short
{
    None      = 0x0000,
    Read      = 0x0001,
    Write     = 0x0002,
    Directory = 0x0004,
    Special   = 0x0008
}

[Flags]
public enum OpenFlags
{
    ReadOnly = 0,
    ReadWrite = 1,
    ReadWriteProtect = 2,
    Directory = 8,
    Create = 10,
    Nonexistent = 20,
    Trincate = 40
}

[Flags]
public enum RemoveFlags
{
    RemoveFile = 0,
    RemoveDir = 1
}