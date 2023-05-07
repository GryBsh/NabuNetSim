namespace Nabu.Network.NHACP.V01;

public class NHACPV01Session : Dictionary<byte, INHACPStorageHandler>, IDisposable
{
    private bool disposedValue;

    public NHACPError LastError { get; set; } = NHACPError.Undefined;
    public string LastErrorMessage = string.Empty;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var slot in this.Values) {
                    slot.End();
                }
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


