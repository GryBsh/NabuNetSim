namespace Nabu.Network.NHACP.V01;

public interface INHACPStorageHandler
{
    Task<(bool, string, int, NHACPError)> Open(OpenFlags flags, string uri);
    Task<(bool, string, byte[], NHACPError)> Get(int offset, int length);
    Task<(bool, string, NHACPError)> Put(int offset, Memory<byte> buffer);
    (bool, int, string, NHACPError) Seek(int offset, NHACPSeekOrigin origin);
    (bool, string, string, NHACPError) Info();
    (bool, int, string, NHACPError) SetSize(int size);
    Task<(bool, string, byte[], NHACPError)> Read(int length);
    Task<(bool, string, NHACPError)> Write(Memory<byte> buffer);
    (bool, string, NHACPError) ListDir(string pattern);
    (bool, string, string, NHACPError) GetDirEntry(byte maxNameLength);
    int Position { get; }
    Task Close();
    void End();
}
