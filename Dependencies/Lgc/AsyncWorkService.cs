using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lgc;

public class AsyncWorkService : IDependency, IDisposable, IAsyncDisposable
{
    private bool disposed;
    readonly Dictionary<string, AsyncWorker> Workers = new();
    readonly CancellationTokenSource TokenSource = new();
    public void Start(string id, Func<CancellationToken, Task> action)
    {
        var worker = AsyncWorker.From(action, TokenSource.Token);
        Workers.Add(id, worker);
    }
    public void Stop(string id)
    {
        Workers[id].Cancel();
        Workers.Remove(id);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                foreach (var worker in Workers)
                    worker.Value.Dispose();
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

        foreach (var worker in Workers)
            await worker.Value.DisposeAsync();

        GC.SuppressFinalize(this);
        disposed = true;
    }
}
