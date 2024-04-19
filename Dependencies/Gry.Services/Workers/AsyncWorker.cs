using System;
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
    private Func<CancellationToken, Task>? CreateTask { get; set; }    protected Task? Task { get; set;  }
    protected CancellationTokenSource TokenSource { get; }

    protected AsyncWorker(Func<CancellationToken, Task>? task = null, CancellationToken? source = null)
    {
        CreateTask = task;
        TokenSource = source is null ? new() : CancellationTokenSource.CreateLinkedTokenSource(source.Value);        Task = CreateTask?.Invoke(TokenSource.Token);
    }    public bool CancellationRequested => TokenSource.IsCancellationRequested;    public bool Canceled => Task?.IsCanceled is true;    public bool Idle => Task?.IsCompletedSuccessfully is true;    public bool Stopped => Task?.IsCompleted is true;    public bool Faulted => Task?.IsFaulted is true;

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
                finally { Task?.Dispose(); }                TokenSource.Dispose();
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
        if (disposed) return;        Cancel();        // Any Async disposal here        // Normal disposal
        await Task.Run(Dispose);
        GC.SuppressFinalize(this);    }

    public static AsyncWorker From(
        Func<CancellationToken, Task> action,
        CancellationToken? token = null
    )
    {
        return new(action, token);
    }

    public static AsyncWorker From(Action<CancellationToken> action, CancellationToken? token = null)
        => From(t => Task.Run(() => action(t), t), token);
}public record AsyncWorker<T, Y> : AsyncWorker, IAsyncWorker<T, Y>, IObservable<AsyncWorkItem<Y>>{    const int DefaultTimeoutMS = 10;    const int DefaultMaxTimeoutMS = 1000;    readonly Subject<AsyncWorkItem<Y>> Results = new();    public AsyncWorker(Func<T, Task<Y>> action, CancellationToken token) : base(null, token)    {        Action = action;        Task = Worker(token);    }    Func<T, Task<Y>> Action { get; }    IProducerConsumerCollection<AsyncWorkItem<T>> WorkQueue { get; } = new ConcurrentQueue<AsyncWorkItem<T>>();    async Task Worker(CancellationToken token)    {        var timeout = SlidingTimeSpan.From(            DefaultTimeoutMS,            DefaultTimeoutMS,            DefaultMaxTimeoutMS        );        while (token.IsCancellationRequested is false)        {            if (WorkQueue.TryTake(out var work))            {                var result = await Action(work.Value);                Results.OnNext(new(work.Id, result));                timeout.Reset();            }            else            {                await Task.Delay(timeout, token);            }        }    }    public bool Execute(AsyncWorkItem<T> work) => WorkQueue.TryAdd(work);    public IDisposable Subscribe(IObserver<AsyncWorkItem<Y>> observer)        => Results.Subscribe(observer);    public override async ValueTask DisposeAsync()    {        await base.DisposeAsync();        Results.Dispose();    }    public void Next(string id, Action<Y> handler)    {        IDisposable? subscription = null;        subscription = Results.Where(result => result.Id == id)                              .Subscribe(result =>                               {                                   handler(result.Value);                                   subscription?.Dispose();                               });    }    public IDisposable Any(string id, Action<Y> handler)    {        return Results.Where(result => result.Id == id)                      .Subscribe(result => handler(result.Value));    }}public record AsyncWorker<Y> : AsyncWorker<NullValue, Y>, IAsyncWorker<Y>{    public AsyncWorker(Func<Task<Y>> action, CancellationToken token) : base(_ => action(), token)    {    }}