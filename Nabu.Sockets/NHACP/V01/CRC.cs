using InvertedTomato.IO;

namespace Nabu.Network.NHACP.V01;

public static class CRC
{
    public static byte GenerateCRC8(Memory<byte> buffer)
    {
        return CrcAlgorithm.CreateCrc8Wcdma().Append(buffer.ToArray()).ToByteArray()[0];
        //return CrcAlgorithm.CreateCrc8Cdma2000().Append(buffer).ToByteArray()[0];
    }
}
