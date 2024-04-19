﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gry.Workers;

public record AsyncWorker : IAsyncWorker
{
    private bool disposed;
    private Func<CancellationToken, Task>? CreateTask { get; set; }
    protected CancellationTokenSource TokenSource { get; }

    protected AsyncWorker(Func<CancellationToken, Task>? task = null, CancellationToken? source = null)
    {
        CreateTask = task;
        TokenSource = source is null ? new() : CancellationTokenSource.CreateLinkedTokenSource(source.Value);
    }

    public void Cancel()
    {
        if (TokenSource.IsCancellationRequested is false &&
            Task?.IsCompleted is false
        ) TokenSource.Cancel();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                Cancel();
                try { if (Task?.IsCompleted is false) Task?.Wait(); }
                catch (TaskCanceledException) { }
                finally { Task?.Dispose(); }
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

    public virtual async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await Task.Run(Dispose);
        GC.SuppressFinalize(this);

    public static AsyncWorker From(
        Func<CancellationToken, Task> action,
        CancellationToken? token = null
    )
    {
        return new(action, token);
    }

    public static AsyncWorker From(Action<CancellationToken> action, CancellationToken? token = null)
        => From(t => Task.Run(() => action(t), t), token);
}