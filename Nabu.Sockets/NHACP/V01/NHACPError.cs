namespace Nabu.Network.NHACP.V01;

public enum NHACPError : short
{
    Undefined = 0,
    NotSupported = 1,
    NotPermitted = 2,
    NotFound = 3,
    IOError = 4,
    BadDescriptor = 5,
    OutOfMemory = 6,
    AccessDenied = 7,
    Busy = 8,
    Exists = 9,
    IsDirectory = 10,
    InvalidRequest = 11,
    TooManyOpen = 12,
    TooLarge = 13,
    OutOfSpace = 14,
    NoSeek = 15,
    NotADirectory = 16,
    NotEmpty = 17,
    NoSuchSession = 18,
    TooManySessions = 19,
    TryAgain = 20,
    WriteProtected = 21
}
