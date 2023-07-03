﻿using System.Text;

namespace Nabu.Network
{
    public interface IFileCache
    {
        Task CacheFile(string path, Memory<byte> content, bool write = true);

        Task CacheString(string path, string content, bool write = true, Encoding? encoding = null);

        Task<Memory<byte>> GetBytes(string path);

        Task<string> GetString(string path);

        DateTime LastChange(string path);

        void Uncache(string path);

        void UncachePath(string path);
    }
}