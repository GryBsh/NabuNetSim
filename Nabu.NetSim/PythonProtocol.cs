using Microsoft.Extensions.Hosting;
using Python.Runtime;
using Nabu.Adaptor;
using Python.Included;
using static System.Formats.Asn1.AsnWriter;
using System;
using Nabu.Patching;

namespace Nabu;

public class PythonProtocol : Protocol
{
    ProtocolSettings Protocol { get; }
    
    public PythonProtocol(IConsole<PythonProtocol> logger, ProtocolSettings protoSettings) : base(logger)
    {
        Protocol = protoSettings;
        Commands = Protocol.Commands;
        
    }

    public override byte[] Commands { get; }
    public override byte Version { get; } = 0x01;

    class PyProto : Protocol
    {
        public PyProto(IConsole logger) : base(logger)
        {
        }

        public override byte Version { get; } = 0;

        public override byte[] Commands => Array.Empty<byte>();

        public override Task Handle(byte unhandled, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }
    }

    public override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        var p = new PyProto(Logger);
        p.Attach(settings, Stream);
        string source = await File.ReadAllTextAsync(Protocol.Path);
        await Task.Run(() =>
        {
            using (var state = Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    try
                    {
                        scope.Set("incoming", unhandled.ToPython());
                        scope.Set("adaptor", p.ToPython());
                        scope.Set("logger", Logger.ToPython());
                        scope.Exec(source);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError(ex.Message, ex);
                    }
                }
            }
            
        }, cancel);
    }


    protected static async Task EnsurePython(IConsole logger)
    {
        Installer.LogMessage += logger.Write;
        var python_installed = Installer.IsPythonInstalled();
        var pip_installed = Installer.IsPipInstalled();

        if (!python_installed) await Installer.SetupPython();
        if (!pip_installed) await Installer.InstallPip();
    }

    public static async Task Startup(IConsole logger)
    {
        Runtime.PythonDLL = Path.Join(Installer.EmbeddedPythonHome, "python311.dll");
        await EnsurePython(logger);
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();
    }

}

