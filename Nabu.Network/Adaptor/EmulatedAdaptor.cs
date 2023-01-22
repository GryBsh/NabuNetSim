using Microsoft.Extensions.Logging;
using Nabu.Network;

namespace Nabu.Adaptor;

public class EmulatedAdaptor : NabuService
{
    //readonly AdaptorSettings Settings;
    readonly Stream Stream;
    readonly BinaryReader Reader;
    readonly ClassicNabuProtocol NabuNet;
    //readonly ACPProtocol ACP;
    IEnumerable<IProtocol> Protocols { get; }

    public EmulatedAdaptor(
        AdaptorSettings settings,
        ClassicNabuProtocol nabu,
        //ACPProtocol acp,
        IEnumerable<IProtocol> protocols,
        ILogger logger,
        Stream stream,
        int index = -1
    ) : base(logger, settings, index)
    {
        Settings = settings;
        NabuNet = nabu;
        Protocols = protocols;

        Stream = stream;
        Reader = new BinaryReader(stream);
        NabuNet.Attach(Settings, Stream);
        foreach (var protocol in Protocols)
            protocol.Attach(Settings, Stream);
    }


    #region Adaptor Loop   

    public virtual async Task Listen(CancellationToken cancel)
    {
        
        Log("Waiting for NABU");
        while (cancel.IsCancellationRequested is false)
        {
            try
            {   
                // Read the command message
                byte incoming = Reader.ReadByte();
                
                // Locate the protocol handler for this command message
                var handler =
                    Protocols.FirstOrDefault(p => p.Commands.Contains(incoming)) ?? // Protocols are bound to specific command messages
                    NabuNet; // Defaults to NABUNet

                if (handler.Attached) // If the handler has been attached to the adapter
                {
                    var shouldContinue = await handler.Listen(incoming, cancel);
                    continue; // Then continue to the next command message
                }

                Trace("Adaptor Loop Break");
                break;
            }
            catch (TimeoutException)
            {
                continue; // Timeouts are normal over serial connections
            }
            catch (IOException ex)
            {
                // There is a big fail in dotnet 7 where Stream throws an IOException
                // instead of a TimeoutException.
                if (ex.HResult == -2147023436) continue; // <-- That's the HResult for a Timeout
                else
                {
                    Log($"Adaptor Loop Error: {ex.Message}");
                    break;
                }
            }
            catch (Exception ex)
            {
                Log($"Adaptor Loop Error: {ex.Message}");
                break;
            }
        }
                
        Log("Disconnected");
       
        NabuNet.Detach();
        foreach (var protocol in Protocols)
            protocol.Detach();
    }
    #endregion

}