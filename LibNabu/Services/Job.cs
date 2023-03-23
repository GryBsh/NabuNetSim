using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
