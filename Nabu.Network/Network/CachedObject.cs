using Nabu.Services;

namespace Nabu.Network;

public record CachedObject(string FilePath, DateTime Timestamp, TimeSpan TTL, byte[] Content) : IDisposable
{
    private bool disposedValue;

    public string FilePath { get; private set; } = FilePath;
    public DateTime Timestamp { get; private set; } = Timestamp;
    public byte[] Content { get; private set; } = Content;
    IConsole? Logger { get; }
    FileSystemWatcher? Watcher { get; set; } = null;
    public CachedObject(
        IConsole logger, 
        string path, 
        DateTime timestamp, 
        TimeSpan ttl, 
        byte[] content, 
        DateTime? lastWrite = null
    ) : this(path, timestamp, ttl, content)
    {
        Logger = logger;
        if (Path.Exists(path))
        {
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                var folder = Path.GetDirectoryName(path);
                var file = Path.GetFileName(path);

                Watcher = new(folder!, file!);

                Watcher.Changed += async (s, e) =>
                {
                    Timestamp = DateTime.Now;
                    Logger.Write($"File {FilePath} changed, reloading");
                    try
                    {
                        while (NabuLib.IsFileAvailable(path) is false)
                            Thread.SpinWait(1);
                        Content = await File.ReadAllBytesAsync(FilePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError(ex.Message, ex);
                    }

                };

                Watcher.Deleted += (s, e) =>
                {
                    Timestamp = DateTime.MinValue;
                    Content = Array.Empty<byte>();
                };

                Watcher.NotifyFilter = NotifyFilters.LastWrite;
                await Task.Delay(TimeSpan.FromSeconds(5));
                Watcher.EnableRaisingEvents = true;
            });
        }
    }

    public void Deconstruct(out DateTime cached, out byte[] content)
    {
        cached = Timestamp;
        content = Content;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Watcher?.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {   // Leave this be.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
