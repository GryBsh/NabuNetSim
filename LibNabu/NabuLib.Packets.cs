//using InvertedTomato.IO;
using Nabu.Services;

namespace Nabu;

public static partial class NabuLib
{

    

    /// <summary>
    ///    Generates the CRC bytes of a packet
    ///    from the CRC table.
    /// </summary>
    /// <param name="buffer">The packet data to generate a CRC for</param>
    /// <returns>The CRC bytes for the packet</returns>
    public static Span<byte> GenerateCRC(Memory<byte> buffer)
    {
        short crc = -1; // 0xFFFF
        foreach (var byt in buffer.ToArray())
        {
            byte b = (byte)(crc >> 8 ^ byt);
            crc <<= 8;
            crc ^= Constants.CRCTable[b];
        }

        

        /*
        var crc = CrcAlgorithm
                  .CreateCrc16Genibus()
                  .Append(buffer)
                  .ToByteArray();
        */

        return new byte[]
        {
            (byte)(crc >> 8 & 0xFF ^ 0xFF),
            (byte)(crc >> 0 & 0xFF ^ 0xFF)
        };

        /*
        return new byte[] {
            (byte)(crc[0] & 0xFF),
            (byte)(crc[1] & 0xFF)
        };
        */
    }

    /// <summary>
    ///     Slices a packet for the requested segment
    ///     from the provided RAW program image
    /// </summary>
    /// <param name="logger">An IConsole instance for provide operational feedback</param>
    /// <param name="segmentIndex">The index of the desired Segment</param>
    /// <param name="pakId">The id of the desired PAK from which to draw the segment</param>
    /// <param name="buffer">The RAW program image data to slice the packet from</param>
    /// <returns></returns>
    public static (bool, byte[]) SliceFromRaw(
        IConsole logger,
        short segmentIndex,
        int pakId,
        Memory<byte> buffer
    )
    {
        int offset = segmentIndex * Constants.MaxPayloadSize;
        if (offset >= buffer.Length)
        {
            logger.WriteError($"Packet Start {offset} is beyond the end of the buffer");
            return (true, Array.Empty<byte>());
        }

        logger.WriteVerbose("Preparing Packet");


        var (next, slice) = Slice(buffer, offset, Constants.MaxPayloadSize);
        bool lastPacket = next is 0;
        int packetSize = slice.Length + Constants.HeaderSize + Constants.FooterSize;
        var message = new byte[packetSize];
        int idx = 0;

        /*
         * 
         * [   Packet   ]
         * [3           ]   Pak ID M-L                  ]
         * [ 1          ]   Segment L                   ]
         * [  1         ]   Owner 0x01                  ]
         * [   4        ]   Tier 0x75 0xFF 0xFF 0xFF    ] - Packet
         * [    2       ]   Mystery 0x7F 0xFF           ] - Header
         * [     1      ]   Type (See code below)       ]
         * [      2     ]   Segment LM                  ]
         * [       2    ]   Offset ML                   ]
         * [      <=991 ]   DATA up to 991 bytes        
         * [           2]   CRC
         * [     END    ]
         * 
         */

        // 16 bytes of header
        message[idx++] = (byte)(pakId >> 16 & 0xFF);              //Pak MSB   
        message[idx++] = (byte)(pakId >> 8 & 0xFF);               //              
        message[idx++] = (byte)(pakId >> 0 & 0xFF);               //Pak LSB   
        message[idx++] = (byte)(segmentIndex & 0xff);                //Segment LSB    
        message[idx++] = 0x01;                                  //Owner         
        message[idx++] = 0x7F;                                  //Tier MSB      
        message[idx++] = 0xFF;
        message[idx++] = 0xFF;
        message[idx++] = 0xFF;                                  //Tier LSB
        message[idx++] = 0x7F;                                  //Mystery Byte
        message[idx++] = 0x80;                                  //Mystery Byte
        message[idx++] = (byte)(                                //Packet Type
                            (lastPacket ? 0x10 : 0x00) |        //bit 4 (0x10) marks End of Segment
                            (segmentIndex == 0 ? 0xA1 : 0x20)
                         );
        message[idx++] = (byte)(segmentIndex >> 0 & 0xFF);           //Segment # LSB
        message[idx++] = (byte)(segmentIndex >> 8 & 0xFF);           //Segment # MSB
        message[idx++] = (byte)(offset >> 8 & 0xFF);            //Offset MSB
        message[idx++] = (byte)(offset >> 0 & 0xFF);            //Offset LSB

        slice.CopyTo(message, idx);         //DATA
        idx += slice.Length;

        //CRC Footer
        var crc = GenerateCRC(message[0..idx]);
        message[idx++] = crc[0];                                //CRC MSB
        message[idx++] = crc[1];                                //CRC LSB

        return (lastPacket, message);
    }

    /// <summary>
    ///    Slices a buffer into a segment and
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="segment"></param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static (bool, byte[]) SliceFromPak(IConsole logger, short segment, Memory<byte> buffer)
    {
        /*
         *  [  Pak   ]
         *  [2       ]  Segment Length (X below) 
         *  [ 16     ]  Header
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
         *  BECAUSE: From a performance standpoint, it's less work, and theoretically
         *  faster.
         */
        
        int length = Constants.TotalPayloadSize;

                     // 2 bytes/seg         //Segment Offset
        int offset = (2 * (segment + 1)) + (segment * length);
        var (next, message) = Slice(buffer, offset, length);
        
        logger.WriteVerbose("Slicing Packet from PAK");
        var crc = GenerateCRC(message[0..^2]);
        message[^2] = crc[0];    //CRC MSB
        message[^1] = crc[1];    //CRC LSB
        return (next is 0, message);
    }
}
