using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Services;
using System.Runtime.InteropServices.JavaScript;

namespace Nabu.JavaScript;

public class JavaScriptProtocol : PluginProtocol
{
    public JavaScriptProtocol(ILog logger, ProtocolSettings protocol) : base(logger, protocol)
    {
    }

    protected override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        if (Protocol is null || !Path.Exists(Protocol.Path))
            return;

        try
        {
            using var engine = new V8ScriptEngine();
            var proxy = new JSAdaptorProxy(this, engine);
            string source = await File.ReadAllTextAsync(Protocol.Path, cancel);
            engine.DocumentSettings.AccessFlags = DocumentAccessFlags.EnableFileLoading;

            engine.AddHostObject("_host_command", HostItemFlags.GlobalMembers, unhandled);
            engine.AddHostObject("_host_adaptor", HostItemFlags.GlobalMembers, proxy);
            engine.AddHostObject("_host_logger", HostItemFlags.GlobalMembers, Logger);

            engine.Execute(
                new DocumentInfo
                {
                    Category = ModuleCategory.Standard
                },
                source
            );
        }
        catch (Exception ex)
        {
            Logger.WriteError(ex.Message, ex);
        }
    }
}