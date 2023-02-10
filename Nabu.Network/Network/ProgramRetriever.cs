namespace Nabu.Network;

public abstract class ProgramRetriever
{
    protected bool IsNabu(string path) => path.EndsWith(".nabu");
    protected bool IsPak(string path) => path.EndsWith(".pak") || IsEncryptedPak(path);
    protected bool IsEncryptedPak(string path) => path.EndsWith(".npak");

    protected AdaptorSettings Settings { get; private set; } = new NullAdaptorSettings();
    public void Attach(AdaptorSettings settings)
    {
        Settings = settings;
    }

    public void Detach() => Settings = new NullAdaptorSettings();
}
