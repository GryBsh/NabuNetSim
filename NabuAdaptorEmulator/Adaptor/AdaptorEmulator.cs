using Microsoft.Extensions.Logging;
using Nabu.Messages;
using Nabu.Network;

using Nabu.Services;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Nabu.Adaptor;

public partial class AdaptorEmulator : NabuService
{
    AdaptorSettings? Settings;
    AdaptorState State;
    Stream Stream;
    BinaryReader Reader;
    BinaryWriter Writer;
    int SendDelay = 0;
    readonly NetworkEmulator Network;
    public AdaptorEmulator(
        NetworkEmulator network,
        ILogger logger,
        Stream stream
    //IStreamAdapter serial
    ) : base(logger)
    {
        Network = network;
        Stream = stream;
        Reader = new BinaryReader(stream, Encoding.ASCII);
        Writer = new BinaryWriter(stream, Encoding.ASCII);
        State = new();
    }

    #region State

    public virtual void OnStart(AdaptorSettings settings)
    {
        Settings = settings;
        State = new()
        {
            Channel = settings.AdapterChannel
        };
        Network.SetState(settings);
        SendDelay = settings.SendDelay ?? 0;
    }

    #endregion

    #region Communication
    public byte Recv()
    {
        return Reader.ReadByte();
    }

    public (bool, byte) Recv(byte byt)
    {
        var (expected, buffer) = Recv(new[] { byt });
        return (expected, buffer[0]);
    }

    public byte[] Recv(int length = 1)
    {
        var buffer = new byte[length];
        for (int i = 0; i < length; i++)
            buffer[i] = Recv();

        Logger.LogTrace($"NA: RCVD: {Format(buffer)}");
        Logger.LogDebug($"NA: RCVD: {buffer.Length} bytes");
        return buffer;
    }

    public (bool, byte[]) Recv(params byte[] bytes)
    {
        var read = Recv(bytes.Length);

        var expected = bytes.SequenceEqual(read);
        if (expected is false)
            Logger.LogWarning($"NA: {Format(bytes)} != {Format(read)}");

        return (
            expected,
            read
        );
    }

    public void Send(params byte[] bytes)
    {
        Logger.LogTrace($"NA: SEND: {Format(bytes)}");
        Writer.Write(bytes, 0, bytes.Length);
        Logger.LogDebug($"NA: SENT: {bytes.Length} bytes");
    }

    public void SlowerSend(params byte[] bytes)
    {
        Logger.LogTrace($"NA: SEND: {Format(bytes)}");
        for (int i = 0; i < bytes.Length; i++)
        {
            Writer.Write(bytes[i]);
            Thread.SpinWait(SendDelay);
        }

        Logger.LogDebug($"NA: SENT: {bytes.Length} bytes");
    }

    #endregion

    #region Adaptor Loop   

    public virtual async Task Emulate(CancellationToken cancel)
    {
        Log("Waiting for NABU");
        while (cancel.IsCancellationRequested is false)
        {
            try
            {
                byte incoming = Recv();
                var cont = incoming switch
                {
                    0xAF => await ACPHandler(cancel),
                    _ => await NabuNetHandler(cancel, incoming)
                };

                if (cont) continue;
                break;

            }
            catch (TimeoutException)
            {
                //Trace("Timeout expired.");
                continue;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                break;
            }
        }

        Log("Disconnected");
        GC.Collect();
    }
    #endregion

}