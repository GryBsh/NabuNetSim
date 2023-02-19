using System.IO;

namespace Nabu;

public enum SourceType
{
    Unknown = 0,
    Local,
    Remote
}

public enum ImageType
{
    None = 0,
    Raw,
    Pak,
    EncryptedPak
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
    Continue = 0,
    Restart,
    Stop
}
