namespace Nabu.Network;

public record AdaptorFrame(ushort Length, byte[] Data);
public record AdaptorResult<T>(bool IsExpected, T Result);

public interface IAdaptorProxy
{
    AdaptorResult<byte> Read(byte expected);

    byte Read();

    byte[] Read(int length);

    AdaptorResult<byte[]> Read(params byte[] expected);

    AdaptorFrame ReadFrame();

    int ReadInt();

    ushort ReadShort();

    void Write(params byte[] bytes);

    void WriteFrame(params byte[] buffer);

    void WriteFrame(byte header, params Memory<byte>[] buffer);
}