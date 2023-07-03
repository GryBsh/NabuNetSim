namespace Nabu;

/// <summary>
///    Constant values used through out the Emulator.
/// </summary>
public static class Constants
{
    public const int CycleMenuNumber = 1;
    public const string CycleMenuPak = "000001";
    public const string NabuExtension = ".nabu";
    public const string PakExtension = ".pak";
    public const string EncryptedPakExtension = ".npak";

    // Sizes
    public const int MaxSegmentSize = 65536;

    public const int MaxPacketSize = 1024;
    public const short MaxPayloadSize = 991;
    public const short HeaderSize = 16;
    public const short FooterSize = 2;
    public const short TotalPayloadSize = MaxPayloadSize + HeaderSize + FooterSize;

    public const int DefaultSerialSendDelay = 500;
    public const int DefaultTCPSendDelay = 130000;

    public const int DefaultTCPPort = 5816;

    //Included for awareness, not used
    public const int TimePacketSize = HeaderSize + 9 + FooterSize;

    // Encryption Key and init vector (seed) for npak files
    public static byte[] PakKey = { 0x6E, 0x58, 0x61, 0x32, 0x62, 0x79, 0x75, 0x7A };

    public static byte[] PakIV = { 0x0C, 0x15, 0x2B, 0x11, 0x39, 0x23, 0x43, 0x1B };

    // CRC values for lookup. There is probably a way to generate it on the fly.
    public static readonly short[] CRCTable = {
        Convert.ToInt16("0x0000", 16), Convert.ToInt16("0x1021", 16), Convert.ToInt16("0x2042", 16), Convert.ToInt16("0x3063", 16),
        Convert.ToInt16("0x4084", 16), Convert.ToInt16("0x50a5", 16), Convert.ToInt16("0x60c6", 16), Convert.ToInt16("0x70e7", 16),
        Convert.ToInt16("0x8108", 16), Convert.ToInt16("0x9129", 16), Convert.ToInt16("0xa14a", 16), Convert.ToInt16("0xb16b", 16),
        Convert.ToInt16("0xc18c", 16), Convert.ToInt16("0xd1ad", 16), Convert.ToInt16("0xe1ce", 16), Convert.ToInt16("0xf1ef", 16),
        Convert.ToInt16("0x1231", 16), Convert.ToInt16("0x0210", 16), Convert.ToInt16("0x3273", 16), Convert.ToInt16("0x2252", 16),
        Convert.ToInt16("0x52b5", 16), Convert.ToInt16("0x4294", 16), Convert.ToInt16("0x72f7", 16), Convert.ToInt16("0x62d6", 16),
        Convert.ToInt16("0x9339", 16), Convert.ToInt16("0x8318", 16), Convert.ToInt16("0xb37b", 16), Convert.ToInt16("0xa35a", 16),
        Convert.ToInt16("0xd3bd", 16), Convert.ToInt16("0xc39c", 16), Convert.ToInt16("0xf3ff", 16), Convert.ToInt16("0xe3de", 16),
        Convert.ToInt16("0x2462", 16), Convert.ToInt16("0x3443", 16), Convert.ToInt16("0x0420", 16), Convert.ToInt16("0x1401", 16),
        Convert.ToInt16("0x64e6", 16), Convert.ToInt16("0x74c7", 16), Convert.ToInt16("0x44a4", 16), Convert.ToInt16("0x5485", 16),
        Convert.ToInt16("0xa56a", 16), Convert.ToInt16("0xb54b", 16), Convert.ToInt16("0x8528", 16), Convert.ToInt16("0x9509", 16),
        Convert.ToInt16("0xe5ee", 16), Convert.ToInt16("0xf5cf", 16), Convert.ToInt16("0xc5ac", 16), Convert.ToInt16("0xd58d", 16),
        Convert.ToInt16("0x3653", 16), Convert.ToInt16("0x2672", 16), Convert.ToInt16("0x1611", 16), Convert.ToInt16("0x0630", 16),
        Convert.ToInt16("0x76d7", 16), Convert.ToInt16("0x66f6", 16), Convert.ToInt16("0x5695", 16), Convert.ToInt16("0x46b4", 16),
        Convert.ToInt16("0xb75b", 16), Convert.ToInt16("0xa77a", 16), Convert.ToInt16("0x9719", 16), Convert.ToInt16("0x8738", 16),
        Convert.ToInt16("0xf7df", 16), Convert.ToInt16("0xe7fe", 16), Convert.ToInt16("0xd79d", 16), Convert.ToInt16("0xc7bc", 16),
        Convert.ToInt16("0x48c4", 16), Convert.ToInt16("0x58e5", 16), Convert.ToInt16("0x6886", 16), Convert.ToInt16("0x78a7", 16),
        Convert.ToInt16("0x0840", 16), Convert.ToInt16("0x1861", 16), Convert.ToInt16("0x2802", 16), Convert.ToInt16("0x3823", 16),
        Convert.ToInt16("0xc9cc", 16), Convert.ToInt16("0xd9ed", 16), Convert.ToInt16("0xe98e", 16), Convert.ToInt16("0xf9af", 16),
        Convert.ToInt16("0x8948", 16), Convert.ToInt16("0x9969", 16), Convert.ToInt16("0xa90a", 16), Convert.ToInt16("0xb92b", 16),
        Convert.ToInt16("0x5af5", 16), Convert.ToInt16("0x4ad4", 16), Convert.ToInt16("0x7ab7", 16), Convert.ToInt16("0x6a96", 16),
        Convert.ToInt16("0x1a71", 16), Convert.ToInt16("0x0a50", 16), Convert.ToInt16("0x3a33", 16), Convert.ToInt16("0x2a12", 16),
        Convert.ToInt16("0xdbfd", 16), Convert.ToInt16("0xcbdc", 16), Convert.ToInt16("0xfbbf", 16), Convert.ToInt16("0xeb9e", 16),
        Convert.ToInt16("0x9b79", 16), Convert.ToInt16("0x8b58", 16), Convert.ToInt16("0xbb3b", 16), Convert.ToInt16("0xab1a", 16),
        Convert.ToInt16("0x6ca6", 16), Convert.ToInt16("0x7c87", 16), Convert.ToInt16("0x4ce4", 16), Convert.ToInt16("0x5cc5", 16),
        Convert.ToInt16("0x2c22", 16), Convert.ToInt16("0x3c03", 16), Convert.ToInt16("0x0c60", 16), Convert.ToInt16("0x1c41", 16),
        Convert.ToInt16("0xedae", 16), Convert.ToInt16("0xfd8f", 16), Convert.ToInt16("0xcdec", 16), Convert.ToInt16("0xddcd", 16),
        Convert.ToInt16("0xad2a", 16), Convert.ToInt16("0xbd0b", 16), Convert.ToInt16("0x8d68", 16), Convert.ToInt16("0x9d49", 16),
        Convert.ToInt16("0x7e97", 16), Convert.ToInt16("0x6eb6", 16), Convert.ToInt16("0x5ed5", 16), Convert.ToInt16("0x4ef4", 16),
        Convert.ToInt16("0x3e13", 16), Convert.ToInt16("0x2e32", 16), Convert.ToInt16("0x1e51", 16), Convert.ToInt16("0x0e70", 16),
        Convert.ToInt16("0xff9f", 16), Convert.ToInt16("0xefbe", 16), Convert.ToInt16("0xdfdd", 16), Convert.ToInt16("0xcffc", 16),
        Convert.ToInt16("0xbf1b", 16), Convert.ToInt16("0xaf3a", 16), Convert.ToInt16("0x9f59", 16), Convert.ToInt16("0x8f78", 16),
        Convert.ToInt16("0x9188", 16), Convert.ToInt16("0x81a9", 16), Convert.ToInt16("0xb1ca", 16), Convert.ToInt16("0xa1eb", 16),
        Convert.ToInt16("0xd10c", 16), Convert.ToInt16("0xc12d", 16), Convert.ToInt16("0xf14e", 16), Convert.ToInt16("0xe16f", 16),
        Convert.ToInt16("0x1080", 16), Convert.ToInt16("0x00a1", 16), Convert.ToInt16("0x30c2", 16), Convert.ToInt16("0x20e3", 16),
        Convert.ToInt16("0x5004", 16), Convert.ToInt16("0x4025", 16), Convert.ToInt16("0x7046", 16), Convert.ToInt16("0x6067", 16),
        Convert.ToInt16("0x83b9", 16), Convert.ToInt16("0x9398", 16), Convert.ToInt16("0xa3fb", 16), Convert.ToInt16("0xb3da", 16),
        Convert.ToInt16("0xc33d", 16), Convert.ToInt16("0xd31c", 16), Convert.ToInt16("0xe37f", 16), Convert.ToInt16("0xf35e", 16),
        Convert.ToInt16("0x02b1", 16), Convert.ToInt16("0x1290", 16), Convert.ToInt16("0x22f3", 16), Convert.ToInt16("0x32d2", 16),
        Convert.ToInt16("0x4235", 16), Convert.ToInt16("0x5214", 16), Convert.ToInt16("0x6277", 16), Convert.ToInt16("0x7256", 16),
        Convert.ToInt16("0xb5ea", 16), Convert.ToInt16("0xa5cb", 16), Convert.ToInt16("0x95a8", 16), Convert.ToInt16("0x8589", 16),
        Convert.ToInt16("0xf56e", 16), Convert.ToInt16("0xe54f", 16), Convert.ToInt16("0xd52c", 16), Convert.ToInt16("0xc50d", 16),
        Convert.ToInt16("0x34e2", 16), Convert.ToInt16("0x24c3", 16), Convert.ToInt16("0x14a0", 16), Convert.ToInt16("0x0481", 16),
        Convert.ToInt16("0x7466", 16), Convert.ToInt16("0x6447", 16), Convert.ToInt16("0x5424", 16), Convert.ToInt16("0x4405", 16),
        Convert.ToInt16("0xa7db", 16), Convert.ToInt16("0xb7fa", 16), Convert.ToInt16("0x8799", 16), Convert.ToInt16("0x97b8", 16),
        Convert.ToInt16("0xe75f", 16), Convert.ToInt16("0xf77e", 16), Convert.ToInt16("0xc71d", 16), Convert.ToInt16("0xd73c", 16),
        Convert.ToInt16("0x26d3", 16), Convert.ToInt16("0x36f2", 16), Convert.ToInt16("0x0691", 16), Convert.ToInt16("0x16b0", 16),
        Convert.ToInt16("0x6657", 16), Convert.ToInt16("0x7676", 16), Convert.ToInt16("0x4615", 16), Convert.ToInt16("0x5634", 16),
        Convert.ToInt16("0xd94c", 16), Convert.ToInt16("0xc96d", 16), Convert.ToInt16("0xf90e", 16), Convert.ToInt16("0xe92f", 16),
        Convert.ToInt16("0x99c8", 16), Convert.ToInt16("0x89e9", 16), Convert.ToInt16("0xb98a", 16), Convert.ToInt16("0xa9ab", 16),
        Convert.ToInt16("0x5844", 16), Convert.ToInt16("0x4865", 16), Convert.ToInt16("0x7806", 16), Convert.ToInt16("0x6827", 16),
        Convert.ToInt16("0x18c0", 16), Convert.ToInt16("0x08e1", 16), Convert.ToInt16("0x3882", 16), Convert.ToInt16("0x28a3", 16),
        Convert.ToInt16("0xcb7d", 16), Convert.ToInt16("0xdb5c", 16), Convert.ToInt16("0xeb3f", 16), Convert.ToInt16("0xfb1e", 16),
        Convert.ToInt16("0x8bf9", 16), Convert.ToInt16("0x9bd8", 16), Convert.ToInt16("0xabbb", 16), Convert.ToInt16("0xbb9a", 16),
        Convert.ToInt16("0x4a75", 16), Convert.ToInt16("0x5a54", 16), Convert.ToInt16("0x6a37", 16), Convert.ToInt16("0x7a16", 16),
        Convert.ToInt16("0x0af1", 16), Convert.ToInt16("0x1ad0", 16), Convert.ToInt16("0x2ab3", 16), Convert.ToInt16("0x3a92", 16),
        Convert.ToInt16("0xfd2e", 16), Convert.ToInt16("0xed0f", 16), Convert.ToInt16("0xdd6c", 16), Convert.ToInt16("0xcd4d", 16),
        Convert.ToInt16("0xbdaa", 16), Convert.ToInt16("0xad8b", 16), Convert.ToInt16("0x9de8", 16), Convert.ToInt16("0x8dc9", 16),
        Convert.ToInt16("0x7c26", 16), Convert.ToInt16("0x6c07", 16), Convert.ToInt16("0x5c64", 16), Convert.ToInt16("0x4c45", 16),
        Convert.ToInt16("0x3ca2", 16), Convert.ToInt16("0x2c83", 16), Convert.ToInt16("0x1ce0", 16), Convert.ToInt16("0x0cc1", 16),
        Convert.ToInt16("0xef1f", 16), Convert.ToInt16("0xff3e", 16), Convert.ToInt16("0xcf5d", 16), Convert.ToInt16("0xdf7c", 16),
        Convert.ToInt16("0xaf9b", 16), Convert.ToInt16("0xbfba", 16), Convert.ToInt16("0x8fd9", 16), Convert.ToInt16("0x9ff8", 16),
        Convert.ToInt16("0x6e17", 16), Convert.ToInt16("0x7e36", 16), Convert.ToInt16("0x4e55", 16), Convert.ToInt16("0x5e74", 16),
        Convert.ToInt16("0x2e93", 16), Convert.ToInt16("0x3eb2", 16), Convert.ToInt16("0x0ed1", 16), Convert.ToInt16("0x1ef0", 16)
    };
}