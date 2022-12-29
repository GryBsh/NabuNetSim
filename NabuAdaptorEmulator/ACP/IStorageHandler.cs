namespace Nabu.Network;

public interface IStorageHandler
{
    string Protocol { get; }
    
    Task<(bool, string, int)> Open(short flags, string uri);
    (bool, string, byte[]) Get(int offset, short length);
    (bool, string) Put(int offset, byte[] buffer);
    
    (bool, string, byte, byte[]) Command(byte index, byte command, byte[] data);
    void End();
}
