using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Nabu.Adaptor;
using Nabu.Network;
using Nabu.Services;

namespace Nabu.JavaScript;

public class JavaScriptProtocol : PluginProtocol
{
    public JavaScriptProtocol(ILog<JavaScriptProtocol> logger) : base(logger)
    {
        
    }
    byte version = 0x01;
    public override byte Version => version;

    byte[] commands = Array.Empty<byte>();
    public override byte[] Commands => commands;

    public override void Activate(ProtocolSettings settings)
    {
        base.Activate(settings);
        commands = settings.Commands ?? commands;
    }

    protected override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        if (Protocol is null || !Path.Exists(Protocol.Path)) return;

        var proxy = new ProxyProtocol(Logger);
        proxy.Attach(Settings, Stream);
        string source = await File.ReadAllTextAsync(Protocol.Path, cancel);
        try
        {
            using var engine = new V8ScriptEngine();
            var global = new
            {
                incoming = unhandled,
                adaptor = proxy,
                logger = Logger
            };
            engine.AddHostObject("global", HostItemFlags.GlobalMembers, global);
            engine.Execute(source);
        }
        catch (Exception ex)
        {
            Logger.WriteError(ex.Message, ex);
        }
    }
}