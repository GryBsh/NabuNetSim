namespace Nabu.Services;

public abstract class Job : IJob
{
    protected IConsole Logger { get; }
    protected Settings Settings { get; }
    public Job(IConsole logger, Settings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    public abstract void Start();
}

public interface IJob
{
    void Start();
}
