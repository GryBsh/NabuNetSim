namespace Gry.Protocols
{
    public record AdaptorFrame(ushort Length, Memory<byte> Data);
}