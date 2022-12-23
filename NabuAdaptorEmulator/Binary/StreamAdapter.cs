using Microsoft.Extensions.Logging;

namespace Nabu.Binary;

public class StreamAdapter : BinaryAdapter
{
    public StreamAdapter(
        Stream stream,
        ILogger logger
    ) : base(logger)
    {
        Stream = stream;
    }

    public override void Open()
    {
        
        
    }

    public override void Close()
    {
        Stream?.Close();
        Stream?.Dispose();
        Stream = null;
    }
}
