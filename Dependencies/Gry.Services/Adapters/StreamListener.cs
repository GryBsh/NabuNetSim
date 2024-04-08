using Gry;
using Gry.Protocols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gry.Adapters;

public abstract class StreamListener<TDefinition>(
    ILogger logger,
    IServiceScopeFactory scopes
) : Listener(logger, scopes)
    where TDefinition : AdapterDefinition
{

    protected static async Task Listen(
        StreamListener<TDefinition> listener,
        ILogger logger,
        TDefinition settings,
        Stream stream,
        IProtocol<TDefinition>? defaultProtocol,
        IServiceProvider services,
        CancellationToken cancel,
        Func<byte>? reader = null
    )
    {
        var protocols = services.GetServices<IProtocol<TDefinition>>();

        defaultProtocol?.Attach(settings, stream);
        foreach (var protocol in protocols)
            protocol.Attach(settings, stream);

        logger.LogInformation("{}: Ready", settings.Name);
        while (cancel.IsCancellationRequested is false)
        {
            try
            {
                Memory<byte> buffer = new byte[1];
                if (reader is not null)
                {
                    buffer.Span[0] = reader();
                }
                else
                {
                    await stream.ReadExactlyAsync(buffer, cancel);
                }
                var incoming = buffer.Span[0]; //buffer[0];

                var handler = protocols.FirstOrDefault(p => p.ShouldHandle(incoming)) ?? defaultProtocol;
                
                if (handler == defaultProtocol)
                    foreach (var protocol in protocols)
                        protocol.Reset();

                if (handler is null)
                {
                    listener.Send(Unhandled, incoming, cancel);
                    continue;
                }
                else if (await handler.HandleMessage(incoming, cancel))
                    continue;

                logger.LogInformation("{}: Triggered Abort", settings.Name);
                break;
            }
            catch (TimeoutException)
            {
                if (listener.Type is AdapterType.Serial && cancel.IsCancellationRequested is false)
                    continue;// Timeouts are normal over serial connections
                break;
            }
            catch (EndOfStreamException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                // This is also a timeout...
                if (ex.HResult == -2147023436 && listener.Type is AdapterType.Serial) 
                    continue;

                logger.LogError(ex, "{}: IO Error", settings.Name);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{}: Error", settings.Name);
                break;
            }
        }

        logger.LogDebug("{}: Disconnecting", settings.Name);

        // Detach all the protocol handler-s from the Adaptor.
        defaultProtocol?.Detach();
        foreach (var protocol in protocols)
            protocol.Detach();
    }


}
