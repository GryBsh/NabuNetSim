using Microsoft.Extensions.Logging;
using Nabu.ACP;
using Nabu.Messages;
using Nabu.Network;

using Nabu.Services;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Channels;

namespace Nabu.Adaptor;

public class AdaptorEmulator : NabuService
{
    readonly AdaptorSettings Settings;
    readonly Stream Stream;
    readonly BinaryReader Reader;
    readonly NabuNetProtocol NabuNet;
    readonly ACPProtocol ACP;
    //IEnumerable<IProtocolExtension> Protocols { get; }

    public AdaptorEmulator(
        AdaptorSettings settings,
        NabuNetProtocol nabu,
        ACPProtocol acp,
        //IEnumerable<IProtocolExtension> protocols,
        ILogger logger,
        Stream stream
    ) : base(logger)
    {
        Settings = settings;
        NabuNet = nabu;
        ACP = acp;
        //Protocols = protocols;
        
        Stream = stream;
        Reader = new BinaryReader(stream);
        NabuNet.Attach(Settings, Stream);
        ACP.Attach(Settings, Stream);
        //foreach (var p in Protocols)
        //    p.Attach(Settings, Stream);
    }


    #region Adaptor Loop   
        
    public virtual async Task Emulate(CancellationToken cancel)
    {
        Log("Waiting for NABU");
        while (cancel.IsCancellationRequested is false)
        {
            
            try
            {
                byte incoming = Reader.ReadByte();
                /*
                var handler = 
                    Protocols.FirstOrDefault(p => p.Identifier == incoming) ??
                    NabuNet;
                */

                IProtocol handler = incoming switch
                {
                    SupportedProtocols.ACP => ACP,
                    _ => NabuNet
                };

                if (handler.Attached &&
                    await handler.Listen(cancel, incoming)
                ) {
                    continue;
                }
                break;
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                GC.Collect();
                break;
            }

        }
    

        NabuNet.Detach();
        ACP.Detach();
        //foreach (var p in Protocols)
        //    p.Detach();

        Log("Disconnected");
        GC.Collect();
    }
    #endregion

}