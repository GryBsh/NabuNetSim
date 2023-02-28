using Microsoft.Extensions.Logging;
using Nabu.Services;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Nabu;

public abstract class NabuService : NabuBase
{
  
    protected AdaptorSettings Settings { get; set; }
    public NabuService(IConsole logger, AdaptorSettings settings) : base(logger)
    {
        
        Settings = settings;
       
    }
    
    protected override string LogMessage(string message)
    {
        return $"{Settings.Type}:{Settings.Port}:{message}";
    }

    

}


public class PeriodicBackgroundWorker
{
    List<IDisposable> ScheduledTasks { get; } = new();

    public int ScheduleTask(TimeSpan period, Action action)
    {
        var index = ScheduledTasks.Count;
        ScheduledTasks.Add(
            Observable.Interval(period, ThreadPoolScheduler.Instance).Subscribe(_ => action())
        );
        return index;
    }

    public void EndSchedule(int index) 
    {
        ScheduledTasks[index].Dispose();
        ScheduledTasks.RemoveAt(index);
    }

}