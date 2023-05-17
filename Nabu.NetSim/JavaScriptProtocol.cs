namespace Nabu;

/*
public class JavaScriptProtocol : Protocol
{
    ProtocolSettings Protocol { get; }

    public JavaScriptProtocol(IConsole<JavaScriptProtocol> logger, ProtocolSettings settings) : base(logger)
    {
        Protocol = settings;
        Commands = Protocol.Commands;
    }

    public override byte Version { get; } = 0x01;

    public override byte[] Commands { get; }

    protected override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        var proxy = new ProxyProtocol(Logger);
        proxy.Attach(Settings, Stream);
        string source = await File.ReadAllTextAsync(Protocol.Path, cancel);
        try
        {
            using var engine =
                new Engine().SetValue("incoming", unhandled)
                            .SetValue("adaptor", proxy)
                            .SetValue("logger", Logger);
            
            engine.Execute(source);
        }
        catch (Exception ex)
        {
            Logger.WriteError(ex.Message, ex);
        }
    }
}
*/

