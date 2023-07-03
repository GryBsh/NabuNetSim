using Nabu.Network;
using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.Adaptor;

/// <summary>
/// The emulated adaptor, wrapping the actual communication Stream to the NABU PC
/// </summary>
public class EmulatedAdaptor : NabuBase
{
    private readonly AdaptorSettings Settings;
    private readonly Stream Stream;
    private readonly BinaryReader Reader;
    private readonly ClassicNabuProtocol NabuNet;

    //readonly ACPProtocol ACP;
    private IEnumerable<IProtocol> Protocols { get; }

    public EmulatedAdaptor(
        AdaptorSettings settings,
        ClassicNabuProtocol nabuNet,
        IEnumerable<IProtocol> protocols,
        ILog logger,
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
                ///SetActive();
                // Locate the protocol handler for this command message
                var handler = Protocols.FirstOrDefault(p => p.ShouldAccept(incoming)) ?? NabuNet;

                if (handler == NabuNet)
                    foreach (var protocol in Protocols)
                        protocol.Reset();

                if (await handler.HandleMessage(incoming, cancel))
                {
                    //SetIdle();
                    continue; // Then continue to the next command message
                }
                Trace("Adaptor Loop Break");
                //SetIdle();
                break;
            }
            catch (TimeoutException)
            {
                continue;// Timeouts are normal over serial connections
            }
            catch (IOException ex)
            {
                // There is a big fail in dotnet 7 where Stream throws an IOException
                // instead of a TimeoutException.
                if (ex.HResult == -2147023436) // <-- That's the HResult for a Timeout
                    continue;

                Log($"Adaptor Loop Error: {ex.Message}");
                //SetIdle();
                break;
            }
            catch (Exception ex)
            {
                Log($"Adaptor Loop Error: {ex.Message}");
                //SetIdle();
                break;
            }
            //SetIdle();
        }
        IsRunning = false;
        Log("Disconnected");

        // Detach all the protocol handlers from the Adaptor.
        NabuNet.Detach();
        foreach (var protocol in Protocols)
            protocol.Detach();
        Stream.Dispose();
    }

    #endregion Adaptor Loop
}