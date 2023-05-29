using System.Reactive.Disposables;

namespace Nabu.Services;

public abstract class DisposableBase : IDisposable
{
    private bool disposedValue;
    protected CompositeDisposable Disposables { get; } = new();

    public DisposableBase() { }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Disposing();
                Disposables.Dispose();
            }
            disposedValue = true;
        }
    }
    protected virtual void Disposing() { }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
