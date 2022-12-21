using Nabu.Adaptor;

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
            await Task.Run(() => onStart?.Invoke(settings));
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
            Task.Run(task),
            source
        );
    }

    public static implicit operator Task(NabuService service) => service.Task;
}