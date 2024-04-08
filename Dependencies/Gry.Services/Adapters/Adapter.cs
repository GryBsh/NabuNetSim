using Gry.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Gry.Adapters;

public delegate Task AdapterListener(Adapter adapter, CancellationToken token);
public partial class Adapter : IDisposable
{
    public const string Startup = $"{nameof(Adapter)}_{nameof(Startup)}";
    public const string Shutdown = $"{nameof(Adapter)}_{nameof(Shutdown)}";

    CancellationTokenSource CancelSource { get; set; }
    AdapterListener? Handler { get; set; }
    public Task? CurrentHandler { get; set; }
    public CancellationToken CancellationToken => CancelSource.Token;
    public AdapterDefinition Definition { get; set; } = new NullAdapterDefinition();
    public AdapterState State { get; protected set; } = AdapterState.Stopped;

    ILogger Logger { get; }
    public Adapter(
        ILogger logger, 
        AdapterDefinition options, 
        AdapterListener? handler,  
        //IServiceScopeFactory scopes,
        CancellationToken stopping
    ) 
    {
        Logger = logger;
        Definition = options;
        CancelSource = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        Handler = handler;
    }

    void SetState( AdapterState state )
    {
        State = state;
    }

    /// <summary>
    /// Starts the Adaptor if stopped and not disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        if (State is AdapterState.Starting or AdapterState.Stopping)
        {
            Logger.LogError("Adapter is {}", State.ToString());
            return;
        } 
            

        //CancelSource = CancellationTokenSource.CreateLinkedTokenSource(SourceCancel);
        SetState(AdapterState.Starting);

        if (Handler is null)
        {
            Logger.LogError("Handler is null");
            Fail();
            return;
        }

        try
        {
            CurrentHandler = Handler(this, CancelSource.Token);
            SetState(AdapterState.Running);
            Definition.Adapter = this;
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Adapter Handler Error");
            Fail();
        }
    }

    /// <summary>
    /// Cancels the Adapter Task
    /// timeout for it to complete. 
    /// </summary>
    public void Cancel()
    {
        if (State is AdapterState.Failed or not AdapterState.Running)
        {
            Logger.LogError("Adapter is {}", State.ToString());
            return;
        }
        
        SetState(AdapterState.Stopping);
        try
        {
            CancelSource.Cancel();
            CurrentHandler?
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (OperationCanceledException) { }
        SetState(AdapterState.Stopped);
        Logger.LogInformation("Adapter Canceled");
    }

    public void Fail()
    {
        Cancel();
        SetState(AdapterState.Failed);
    }

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Cancel();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
