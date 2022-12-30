namespace Nabu.ACP;

[Flags]
public enum StorageFlags : short
{
    ReadWrite = 0b_0000_0000_0000_0000, //0
    ReadOnly = 0b_0000_0000_0000_0001, //1
}

[Flags]
public enum HttpStorageFlags : short
{
    FetchOnly = 0b_0000_0000_0000_0000, //0
    Post = 0b_0000_0000_0000_0001, //1
    Patch = 0b_0000_0000_0000_0011, //3
}