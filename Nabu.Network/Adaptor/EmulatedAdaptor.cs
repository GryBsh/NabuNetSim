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
        IConsole logger,
        Stream stream,
        int index = -1
    ) : base(logger, settings, index)
    {
        base.settings = settings;
        NabuNet = nabu;
        Protocols = protocols;

        Stream = stream;
        Reader = new BinaryReader(stream);
        NabuNet.Attach(base.settings, Stream);
        foreach (var protocol in Protocols)
            protocol.Attach(base.settings, Stream);
    }


    #region Adaptor Loop   

    IProtocol? HandlerFor(byte incoming)
    {
        return Protocols.FirstOrDefault(p => p.ShouldAccept(incoming));
    }

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
                var handler = HandlerFor(incoming);
                if (handler is null) handler = NabuNet;
                if (handler?.Attached is true) // If the handler has been attached to the adapter
                {
                    if (handler is ClassicNabuProtocol)
                    {
                        foreach (var p in Protocols)
                            p.Reset();
                    }

                    if (await handler.Listen(incoming, cancel))
                        continue; // Then continue to the next command message
                    else break;
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
                
                Log($"Adaptor Loop Error: {ex.Message}");
                break;
                
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
        Stream.Dispose();
    }
    #endregion

}