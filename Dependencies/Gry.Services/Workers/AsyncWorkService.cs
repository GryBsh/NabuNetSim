using Lgc;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Gry.Workers;

public class AsyncWorkService : ISingletonDependency, IDisposable, IAsyncDisposable
{
    private bool disposed;
    readonly DataDictionary<AsyncWorker> Workers = new();
    readonly CancellationTokenSource TokenSource = new();    public IAsyncWorker Get(string id) => Workers[id];
    public IAsyncWorker? Worker(string id, Func<CancellationToken, Task> action)
    {        if (Workers.TryGetValue(id, out var value))        {            if (value is IAsyncWorker existing) return existing;            return null;        }
        var worker = AsyncWorker.From(action, TokenSource.Token);        return Workers.AddOrUpdate(id, worker, (i, o) => worker);
    }    public IAsyncWorker<T,Y>? Worker<T,Y>(string id, Func<T, Task<Y>> action)    {        if (Workers.TryGetValue(id, out AsyncWorker? value))        {            if (value is IAsyncWorker<T,Y> existing) return existing;            return null;        }        var worker = new AsyncWorker<T,Y>(action, TokenSource.Token);        Workers.AddOrUpdate(id, worker, (i, o) => worker);        return worker;    }    public IAsyncWorker<Y>? Worker<Y>(string id, Func<Task<Y>> action)    {        if (Workers.TryGetValue(id, out AsyncWorker? value))        {            if (value is IAsyncWorker<Y> existing) return existing;            return null;        }        var worker = new AsyncWorker<Y>(action, TokenSource.Token);        Workers.AddOrUpdate(id, worker, (i, o) => worker);        return worker;    }
    public void Cancel(string id)
    {
        Workers[id].Cancel();
        Workers.Remove(id, out var stopped);
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
