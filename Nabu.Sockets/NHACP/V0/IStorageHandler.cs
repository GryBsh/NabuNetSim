namespace Nabu.Network.NHACP.V0;

public interface IStorageHandler
{
    Task<(bool, string, byte, byte[])> Command(byte index, byte command, byte[] data);

    void End();

    Task<(bool, string, Memory<byte>)> Get(int offset, ushort length);

    Task<(bool, string, int)> Open(ushort flags, string uri);

    Task<(bool, string)> Put(int offset, Memory<byte> buffer);
}