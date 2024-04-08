using Gry;


namespace Gry.Jobs;

public abstract class Job : DisposableBase, IJob
{
    CancellationTokenSource CancelSource { get; } = new();
    protected CancellationToken State { get; private set; }
    protected abstract void OnSchedule();

    public void Schedule(CancellationToken token)
    {
        State = CancellationTokenSource.CreateLinkedTokenSource(token, CancelSource.Token).Token;
        OnSchedule();
    }

    public void Cancel()
    {
        CancelSource.Cancel();
        Dispose();
    }
}