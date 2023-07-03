namespace Nabu.Network.RetroNet;

[Flags]
public enum FileSeekFlags
{
    FromBeginning = 1,
    FromCurrent = 2,
    FromEnd = 3
}

[Flags]
public enum FileListFlags
{
    Files = 0b_0000_0001,
    Directories = 0b_0000_0010
}

[Flags]
public enum CopyMoveFlags
{
    NoReplace = 0b_0000_0000,
    Replace = 0b_0000_0001,
}

[Flags]
public enum FileOpenFlags : short
{
    ReadOnly = 0b_0000_0000,
    ReadWrite = 0b_0000_0001,
}