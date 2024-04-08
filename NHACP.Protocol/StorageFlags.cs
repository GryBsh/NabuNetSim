namespace NHACP;

[Flags]
public enum StorageFlags : short
{
    ReadWrite = 0x0000,
    ReadOnly = 0x0001, //1
    Create = 0x0002, //2
}

[Flags]
public enum HttpFlags : short
{
    FetchOnly = 0b_0000000000000000, //0
    Post = 0b_0000000000000001, //1
    Patch = 0b_0000000000000011, //3
}

public static class NHACPConstants
{
    public const ushort MaxDataSize = 8192;
}