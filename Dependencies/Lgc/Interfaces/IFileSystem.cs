using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Lgc;

public interface IFileSystem
{
    IFileInfo GetFileInfo(string path);
    bool Exists(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
}
