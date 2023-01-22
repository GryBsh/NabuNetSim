namespace Nabu.Network;

public abstract class ProgramRetriever
{
    protected bool IsNabu(string path) => path.EndsWith(".nabu");
    protected bool IsPak(string path) => path.EndsWith(".pak") || IsEncryptedPak(path);
    protected bool IsEncryptedPak(string path) => path.EndsWith(".npak");
}
