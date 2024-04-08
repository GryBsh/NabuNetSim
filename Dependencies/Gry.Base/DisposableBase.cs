using System.Reactive.Disposables;

namespace Gry;

public abstract class DisposableBase : IDisposable
{
    private bool disposedValue;
    protected CompositeDisposable Disposables { get; } = new();

    public DisposableBase()
    { }

    private void Dispose(bool disposing)
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

    protected virtual void Disposing()
    { }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}