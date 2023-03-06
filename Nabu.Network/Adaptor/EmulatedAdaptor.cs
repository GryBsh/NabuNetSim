using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Services;

namespace Nabu.Adaptor;


/// <summary>
/// The emulated adaptor, wrapping the actual communication Stream to the NABU PC
/// </summary>
public class EmulatedAdaptor : NabuBase
{
    readonly AdaptorSettings Settings;
    readonly Stream Stream;
    readonly BinaryReader Reader;
    readonly ClassicNabuProtocol NabuNet;
    //readonly ACPProtocol ACP;
    IEnumerable<IProtocol> Protocols { get; }

    public EmulatedAdaptor(
        AdaptorSettings settings,
        ClassicNabuProtocol nabuNet,
        IEnumerable<IProtocol> protocols,
        IConsole logger,
        Stream stream,
        string? label = null
       
    ) : base(logger, label)
    {
        Settings = settings;
        NabuNet = nabuNet;
        Protocols = protocols;
        Stream = stream;
        Reader = new BinaryReader(stream);
        NabuNet.Attach(Settings, Stream);
        foreach (var protocol in Protocols)
            protocol.Attach(Settings, Stream);
    }

    public bool IsRunning { get; protected set; }

    #region Adaptor Loop   

    public virtual async Task Listen(CancellationToken cancel)
    {
        IsRunning = true;
        Log("Waiting for NABU");
        
        while (cancel.IsCancellationRequested is false)
        {
            try
            {   
                // Read the command message
                byte incoming = Reader.ReadByte();

                // Locate the protocol handler for this command message
                var handler = Protocols.FirstOrDefault(p => p.ShouldAccept(incoming));
                if (handler is null) handler = NabuNet;
                if (handler?.Attached is true) // If the handler has been attached to the adapter
                {
                    if (handler is ClassicNabuProtocol)
                    {
                        foreach (var p in Protocols) p.Reset();
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
                if (ex.HResult == -2147023436) // <-- That's the HResult for a Timeout
                    continue; 
                
                Log($"Adaptor Loop Error: {ex.Message}");
                break;
                
            }
            catch (Exception ex)
            {
                Log($"Adaptor Loop Error: {ex.Message}");
                break;
            }
        }
        IsRunning = false;
        Log("Disconnected");
       
        // Detach all the protocol handlers from the Adaptor.
        NabuNet.Detach();
        foreach (var protocol in Protocols)
            protocol.Detach();
        Stream.Dispose();
    }
    #endregion

}