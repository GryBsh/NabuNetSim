using Nabu.Adaptor;

namespace Nabu.Services;

public sealed class NabuServiceTask
{
    readonly CancellationTokenSource TokenSource;
    public Task Task { get; }
    public CancellationToken CancellationToken => TokenSource.Token;

    private NabuServiceTask(Task task, CancellationTokenSource source)
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

    public static NabuServiceTask From(
        Func<CancellationToken, Task> action,
        AdaptorSettings settings,
        CancellationToken? token = null,
        Action<AdaptorSettings>? onStart = null,
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
            await Task.Run(() => onStart?.Invoke(settings), source.Token);
            await action(source.Token);
            await Task.Run(() => onStop?.Invoke(), source.Token);
        }

        return new(
            Task.Run(task, source.Token),
            source
        );
    }

    public static NabuServiceTask From(
        Action<CancellationToken> action,
        AdaptorSettings settings,
        CancellationToken? token = null,
        Action<AdaptorSettings>? onStart = null,
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
            onStart?.Invoke(settings);
            action(source.Token);
            onStop?.Invoke();
        }

        return new(
            Task.Run(task, source.Token),
            source
        );
    }

    public static implicit operator Task(NabuServiceTask service) => service.Task;
}