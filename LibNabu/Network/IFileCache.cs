using System.Text;

namespace Nabu.Network
{
    public interface IFileCache
    {
        void CacheFile(string path, Memory<byte> content, bool write = true);
        void CacheString(string path, string content, bool write = true, Encoding? encoding = null);
        Task<Memory<byte>> GetFile(string path);
        Task<string> GetString(string path);
        DateTime LastChange(string path);
        void UnCache(string path);
    }
}