using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.Services;

public static class DisposableExtensions
{
    public static CompositeDisposable AddInterval(
        this CompositeDisposable disposables,
        TimeSpan interval,
        Action<long> action,
        IScheduler? scheduler = null
    )
    {
        scheduler ??= TaskPoolScheduler.Default;

        disposables.Add(
            Observable.Interval(interval, scheduler).Subscribe(action)
        );

        return disposables;
    }
}