namespace Gry.Jobs;

public interface IJob : IDisposable
{
    void Schedule(CancellationToken stopping);
    void Cancel();
}