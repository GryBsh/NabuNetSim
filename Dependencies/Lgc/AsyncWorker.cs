using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lgc;

public record AsyncWorker : IDisposable, IAsyncDisposable
{
    private bool disposed;
    private readonly Task Task;
    private readonly CancellationTokenSource TokenSource;

    private AsyncWorker(Task task, CancellationTokenSource source)
    {
        Task = task;
        TokenSource = source;
    }

    public void Cancel()
    {
        if (TokenSource.IsCancellationRequested is false &&
            Task.IsCompleted is false
        )   TokenSource.Cancel();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                Cancel();
                try { if (Task.IsCompleted is false) Task.Wait(); }
                catch (TaskCanceledException) { }
                finally { Task.Dispose(); }
                TokenSource.Dispose();
            }

            // NATIVE HERE
            disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        
        Cancel();
        try { if (Task.IsCompleted is false) await Task.Run(() => Task.Wait()); }
        catch (TaskCanceledException) { }
        finally { Task.Dispose(); }
        TokenSource.Dispose();
        GC.SuppressFinalize(this);
        
    }

    public static AsyncWorker From(
        Func<CancellationToken, Task> action,
        CancellationToken? token = null
    )
    {
        var source = token is null ?
            new CancellationTokenSource() :
            CancellationTokenSource.CreateLinkedTokenSource(
                (CancellationToken)token
            );

        return new(
            action(source.Token),
            source
        );
    }

    public static AsyncWorker From(Action action, CancellationToken? token = null)
        => From(t => Task.Run(action, t), token);
}