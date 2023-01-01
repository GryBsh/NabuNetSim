using Microsoft.Extensions.Logging;
using Nabu.Network;

namespace Nabu.Adaptor;

public class EmulatedAdaptor : NabuService
{
    readonly AdaptorSettings Settings;
    readonly Stream Stream;
    readonly BinaryReader Reader;
    readonly NabuNetProtocol NabuNet;
    //readonly ACPProtocol ACP;
    IEnumerable<IProtocol> Protocols { get; }

    public EmulatedAdaptor(
        AdaptorSettings settings,
        NabuNetProtocol nabu,
        //ACPProtocol acp,
        IEnumerable<IProtocol> protocols,
        ILogger logger,
        Stream stream
    ) : base(logger)
    {
        Settings = settings;
        NabuNet = nabu;
        //ACP = acp;
        Protocols = protocols;

        Stream = stream;
        Reader = new BinaryReader(stream);
        NabuNet.Attach(Settings, Stream);
        //ACP.Attach(Settings, Stream);
        foreach (var protocol in Protocols)
            protocol.Attach(Settings, Stream);
    }


    #region Adaptor Loop   
    
    void Cleanup(Task? trigger = null)
    {
        bool idle = trigger is not null;
        Log($"Cleanup triggered, idle: {idle}");
      
        GC.Collect();
        if (idle) return;
        
        // Shutdown...
        NabuNet.Detach();
        foreach (var protocol in Protocols)
            protocol.Detach();
    }

    public virtual async void HandleConnection(CancellationToken token)
        => await WaitRun(token);

    public virtual async Task WaitRun(CancellationToken cancel)
    {
        Task idleCleanup = Task.CompletedTask;
        CancellationTokenSource idle = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        
        Log("Waiting for NABU");
        while (cancel.IsCancellationRequested is false)
        {
            try
            {
                if (!idleCleanup.IsCompleted) idle.Cancel(); // Cancel the idle cleanup task
                idleCleanup = Task.Delay(60000, idle.Token)
                                  .ContinueWith(Cleanup, idle.Token);

                // Read the command message
                byte incoming = Reader.ReadByte();
                
                // Locate the protocol handler for this command message
                var handler =
                    Protocols.FirstOrDefault(p => p.Command == incoming) ?? // Protocols are bound to specific command messages
                    NabuNet; // Defaults to NABUNet

                if (handler.Attached && // If the handler has been attached to the adapter
                    await handler.Listen(cancel, incoming) // And the handler does not signal an abort.
                ) continue; // Then continue to the next command message

                Trace("Adaptor Loop Break");
                break;
            }
            catch (TimeoutException)
            {
                continue; // Timeouts are normal over serial connections
            }
            catch (EndOfStreamException ex)
            {
                idle.Cancel();
                Cleanup();
                throw ex;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                break;
            }
        }
                
        Log("Disconnected");
        idle.Cancel();
        Cleanup();
    }
    #endregion

}