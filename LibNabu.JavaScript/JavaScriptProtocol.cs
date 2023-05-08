using Microsoft.ClearScript.V8;
using Nabu;
using Nabu.Adaptor;
using Nabu.Network;
using Nabu.Services;

namespace Nabu.JavaScript;

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
            using var engine = new V8ScriptEngine();
            engine.AddHostObject("incoming", unhandled);
            engine.AddHostObject("adaptor", proxy);
            engine.AddHostObject("logger", Logger);
            engine.Execute(source);
        }
        catch (Exception ex)
        {
            Logger.WriteError(ex.Message, ex);
        }
    }
}