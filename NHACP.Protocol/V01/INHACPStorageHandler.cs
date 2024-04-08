namespace NHACP.V01
{
    public interface INHACPStorageHandler
    {
        Task<(bool, string, uint, NHACPErrors)> Open(NHACPOpenFlags flags, string uri);

        Task<(bool, string, Memory<byte>, NHACPErrors)> Get(uint offset, uint length, bool realLength = false);

        Task<(bool, string, NHACPErrors)> Put(uint offset, Memory<byte> buffer);

        (bool, uint, string, NHACPErrors) Seek(uint offset, NHACPSeekOrigin origin);

        (bool, string, string, NHACPErrors) Info();

        (bool, uint, string, NHACPErrors) SetSize(uint size);

        Task<(bool, string, Memory<byte>, NHACPErrors)> Read(uint length);

        Task<(bool, string, NHACPErrors)> Write(Memory<byte> buffer);

        (bool, string, NHACPErrors) ListDir(string pattern);

        (bool, string?, string, NHACPErrors) GetDirEntry(byte maxNameLength);

        uint Position { get; }

        Task Close();

        void End();
    }
}
