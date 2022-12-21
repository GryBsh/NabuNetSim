namespace Nabu.Network;

public enum ChannelType
{
    Unknown = 0,
    LocalNabu,
    RemoteNabu,
    LocalPak,
    RemotePak
}

public enum ImageType
{
    None = 0,
    Nabu,
    Pak
}

public record Channel(
    string Name, 
    string Source, 
    ChannelSourceType SourceType,
    string Path, 
    ChannelType Type
);
