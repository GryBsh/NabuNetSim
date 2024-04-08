using Lgc;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Protocols;
using Nabu.Settings;
using Napa;
using System.Runtime.InteropServices.JavaScript;
using YamlDotNet.Serialization;

namespace Nabu.JavaScript;

public class JavaScriptProtocolModule : Module;
public class ModuleBuilder: IRegister
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var settings = new GlobalSettings();
        configuration.Bind("Settings", settings);
        var factory = new JavaScriptFactory();
        services.AddSingleton<IProtocolFactory<AdaptorSettings>>(factory);
        foreach (var protocol in settings.Protocols)
            if (protocol.Type.Equals(factory.Type, StringComparison.InvariantCultureIgnoreCase))
                services.AddTransient(
                    sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<PluginProtocol>>();
                        return factory.CreateProtocol(sp, logger, protocol);
                    }
                );
    }
}

public class JavaScriptProtocol(ILogger logger, ProtocolSettings protocol) : PluginProtocol(logger, protocol)
{
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
            Logger.LogError(ex, "JS Handler Failed");
        }
    }
}