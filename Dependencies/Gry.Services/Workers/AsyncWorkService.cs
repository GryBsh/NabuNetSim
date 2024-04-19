﻿using Lgc;
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
    readonly CancellationTokenSource TokenSource = new();
    public IAsyncWorker? Worker(string id, Func<CancellationToken, Task> action)
    {
        var worker = AsyncWorker.From(action, TokenSource.Token);
    }
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