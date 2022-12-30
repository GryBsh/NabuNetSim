using Microsoft.Extensions.Logging;

namespace Nabu;

public static partial class NABU
{
    public static byte[] GenerateCRC(byte[] buffer)
    {
        short crc = -1; // 0xFFFF
        foreach (var byt in buffer)
        {
            byte b = (byte)(crc >> 8 ^ byt);
            crc <<= 8;
            crc ^= Constants.CRCTable[b];
        }
        return new byte[] {
        (byte)(crc >> 8 & 0xFF ^ 0xFF),
        (byte)(crc >> 0 & 0xFF ^ 0xFF)
    };

    }

    /// <summary>
    ///     Creates a packet fpr the requested segment
    ///     from the provided buffer
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="emulator"></param>
    /// <param name="segment"></param>
    /// <param name="pak"></param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static (bool, byte[]) SliceRaw(
        ILogger logger,
        short segment,
        int pak,
        byte[] buffer
    )
    {
        int offset = segment * Constants.MaxPayloadSize;
        if (offset >= buffer.Length)
        {
            logger.LogError($"Packet Start {offset} is beyond the end of the buffer");
            return (true, Array.Empty<byte>());
        }

        logger.LogTrace("Preparing Packet");


        var (next, slice) = SliceArray(buffer, offset, Constants.MaxPayloadSize);
        bool lastPacket = next is 0;
        int packetSize = slice.Length + Constants.HeaderSize + Constants.FooterSize;
        var message = new byte[packetSize];
        int idx = 0;

        /*
         * [   Packet   ]
         * [3           ]   Pak ID M-L
         * [ 1          ]   Segment L
         * [  1         ]   Owner 0x01
         * [   4        ]   Tier 0x75 0xFF 0xFF 0xFF
         * [    2       ]   Mystery 0x7F 0xFF
         * [     1      ]   Type (See code below)
         * [      2     ]   Segment LM
         * [       2    ]   Offset ML
         * [      >=991 ]   DATA upto 991 bytes
         * [           2]   CRC
         * [     END    ]
         */

        // 16 bytes of header
        message[idx++] = (byte)(pak >> 16 & 0xFF);              //Pak MSB   
        message[idx++] = (byte)(pak >> 8 & 0xFF);               //              
        message[idx++] = (byte)(pak >> 0 & 0xFF);               //Pak LSB   
        message[idx++] = (byte)(segment & 0xff);                //Segment LSB    
        message[idx++] = 0x01;                                  //Owner         
        message[idx++] = 0x7F;                                  //Tier MSB      
        message[idx++] = 0xFF;
        message[idx++] = 0xFF;
        message[idx++] = 0xFF;                                  //Tier LSB
        message[idx++] = 0x7F;                                  //Mystery Byte
        message[idx++] = 0x80;                                  //Mystery Byte
        message[idx++] = (byte)(                                //Packet Type
                            (lastPacket ? 0x10 : 0x00) |        //bit 4 (0x10) marks End of Segment
                            (segment == 0 ? 0xA1 : 0x20)
                         );
        message[idx++] = (byte)(segment >> 0 & 0xFF);           //Segment # LSB
        message[idx++] = (byte)(segment >> 8 & 0xFF);           //Segment # MSB
        message[idx++] = (byte)(offset >> 8 & 0xFF);            //Offset MSB
        message[idx++] = (byte)(offset >> 0 & 0xFF);            //Offset LSB

        slice.CopyTo(message, idx);         //DATA
        idx += slice.Length;

        //CRC Footer
        var crc = GenerateCRC(message[0..idx]);
        message[idx++] = crc[0];        //CRC MSB
        message[idx++] = crc[1];        //CRC LSB

        return (lastPacket, message);
    }

    public static (bool, byte[]) SlicePak(ILogger logger, short segment, byte[] buffer)
    {
        /*
         *  [  Pak   ]
         *  [2       ]  Segment Length (X below) 
         *  [ 16     ]  Usual Packet Header
         *  [   XXXX ]  DATA
         *  [       2]  CRC
         *  [  NEXT  ]  The above over and over
         *  
         *  All the cycle 1 and 2 PAKs are made the same way you'd
         *  make packets from raw program data, they are 16 + 991 + 2
         *  bytes unescaped until the last one.
         *  
         *  SO: I'm not even going to bother to parse it, I will simply count
         *  the required bytes, and then read the packet, and correct the CRC.
         */

        int length = Constants.TotalPayloadSize;

        int offset = (segment * length) + (2 * (segment + 1));
        var (next, message) = SliceArray(buffer, offset, length);
        
        logger.LogDebug("Sending Packet from PAK");
        var crc = GenerateCRC(message[0..^2]);
        message[^2] = crc[0];    //CRC MSB
        message[^1] = crc[1];    //CRC LSB
        return (next is 0, message);
    }
}