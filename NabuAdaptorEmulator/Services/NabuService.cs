using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Services;

public sealed class NabuService
{
    readonly CancellationTokenSource TokenSource;
    public Task Task { get; }
    public CancellationToken CancellationToken => TokenSource.Token;

    private NabuService(Task task, CancellationTokenSource source)
    {
        Task = task;
        TokenSource = source;
    }

    public void Cancel()
    {
        if (TokenSource.IsCancellationRequested is false &&
            Task.IsCompleted is false
        ) TokenSource.Cancel();
    }

    public static NabuService From(
        Func<CancellationToken, Task> action,
        CancellationToken? token = null,
        Action? onStart = null,
        Action? onStop = null
    )
    {
        var source = token is null ?
            new CancellationTokenSource() :
            CancellationTokenSource.CreateLinkedTokenSource(
                (CancellationToken)token
            );

        async Task task()
        {
            await Task.Run(() => onStart?.Invoke());
            await action(source.Token);
            await Task.Run(() => onStop?.Invoke());
        }

        return new(
            task(),
            source
        );
    }

    public static NabuService From(
        Action<CancellationToken> action,
        CancellationToken? token = null,
        Action? onStart = null,
        Action? onStop = null
    )
    {
        var source = token is null ?
                     new CancellationTokenSource() :
                     CancellationTokenSource.CreateLinkedTokenSource(
                        (CancellationToken)token
                     );

        void task()
        {
            onStart?.Invoke();
            action(source.Token);
            onStop?.Invoke();
        }

        return new(
            Task.Run(task),
            source
        );
    }

    public TaskAwaiter<NabuService> GetAwaiter()
        => Task.ContinueWith(t => this).GetAwaiter();

}