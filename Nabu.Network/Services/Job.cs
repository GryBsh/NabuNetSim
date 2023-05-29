using Microsoft.Extensions.Logging;

namespace Nabu.Services;

public abstract class Job : DisposableBase, IJob
{
    protected ILog Logger { get; }
    protected Settings Settings { get; }

    public Job(ILog logger, Settings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    public abstract void Start();
}
