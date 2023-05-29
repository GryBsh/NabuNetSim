namespace Nabu;

public enum SourceType
{
    Unknown = 0,
    Local,
    Remote,
    Package
}

[Flags]
public enum RefreshType
{
    Local = 0b_0001,
    Remote = 0b_0010,
    All = Local | Remote
}

public enum ImageType
{
    None = 0,
    Raw,
    Pak,
    EncryptedPak,
    ExploitLoaded
}

public enum AdaptorType
{
    Unknown = 0,
    Serial,
    TCP,
    Relay
}

public enum ServiceShould
{
    Run = 0,
    Restart,
    Stop
}

public interface IEntity
{
    Guid Id { get; }
}