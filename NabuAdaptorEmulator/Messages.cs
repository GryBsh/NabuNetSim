using static System.Formats.Asn1.AsnWriter;

namespace Nabu;

public static class Names
{
    public const string NabuPC = "NPC";
    public const string NetworkAdapter = "NA"; 
}

internal static class Messages
{
    /*
     * I've tried to give distrinct messages some kind of name
     * Not guaranteed to be accurate, likely to change
     */
    public const byte Reset                         = 0x80;
    public const byte MagicalMysteryMessage         = 0x81;
    public const byte GetStatus                     = 0x82;
    public const byte StartUp                       = 0x83;
    public const byte PacketRequest                 = 0x84;
    public const byte ChangeChannel                 = 0x85;
    public const byte Begin                         = 0x8F;

    public const byte Unauthorized                  = 0x90;
    public const byte Authorized                    = 0x91;
    
    public const byte Escape                        = 0x10;
    
    public const byte Ready                         = 0x05;
    public const byte Good                          = 0x06;
    public const byte Confirmed                     = 0xE4;
    public const byte Done                          = 0xE1;



    //public const byte IOPortA = 0x0E;
    //public const byte IOPortB = 0x0F;


    public static readonly byte[] ACK = { Escape, Good };

    public static readonly byte[] Finished = { Escape, Done };

    public const int TimePak = 0x007FFFFF;
}

public static class ServiceStatus {
    public const byte Unauthorized  = 0x90;
    public const byte Authorized    = 0x91;
}

public static class AdaptorStatus
{
    public const byte Signal = 0x01;
    public const byte Transmit = 0x1E;
}

public static class Status
{
    

    public const byte NoSignal = 0x9F;
    public const byte SignalLock = 0x1F;

}


