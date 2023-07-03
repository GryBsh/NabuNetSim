namespace Nabu.Network.NHACP.V0;

public interface IStorageHandler
{
    Task<(bool, string, int)> Open(short flags, string uri);

    Task<(bool, string, Memory<byte>)> Get(int offset, short length);

    Task<(bool, string)> Put(int offset, Memory<byte> buffer);

    Task<(bool, string, byte, byte[])> Command(byte index, byte command, byte[] data);

    void End();
}