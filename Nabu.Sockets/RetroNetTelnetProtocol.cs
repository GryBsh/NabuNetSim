using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace Nabu.Network;

public class RetroNetTelnetProtocol : Protocol
{
    public RetroNetTelnetProtocol(ILogger<RetroNetTelnetProtocol> logger) : base(logger)
    {
    }

    public override byte Command => 0xA6;

    protected override byte Version => 0x01;

    void Write(string message) => Send(NabuLib.FromASCII(message).ToArray());
    void WriteLine(string? message = null) => Write($"{message}\n");
    char[] Read() => Encoding.ASCII.GetChars(Recv(1));
    byte[] FromCharacters(params char[] chr) => Encoding.ASCII.GetBytes(chr);

    string ReadLine(bool echoOff = false, char? replacement = null)
    {
        var incoming = Array.Empty<char>();
        var line = string.Empty;
        while (true)
        {
            incoming = Read();
            if (incoming[0] == '\r') break;
            else if (incoming[0] is '\b')
            {
                if (!string.IsNullOrEmpty(line))
                    line = new(line.AsSpan()[..^1].ToArray());
                continue;
            }
            else
            {
                line += incoming[0];
                if (echoOff is false)
                {
                    if (replacement is not null) Send(FromCharacters(replacement.Value));
                    else Send((byte)incoming[0]);
                }
            }
        }
        return line;
    }

    string Prompt(string prompt, bool echoOff = false, char? replacement = null)
    {
        Write($"{prompt}: ");
        var input = ReadLine(echoOff, replacement);
        WriteLine();
        return input;
    }
    
    public override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        WriteLine("Socialist Workers Terminal vL.T.S");
        WriteLine("\"It works for us now comrad!\"");
        WriteLine();
        while (cancel.IsCancellationRequested is false)
            try
            {
                var hostname = Prompt("Hostname");
                var port = Prompt("Port");
                WriteLine($"Capitalist Swine Detected, implemeting protocols...");
                Log($"Opening Socket to {hostname}:{port}...");
                WriteLine($"Connecting to {hostname}:{port}");
                using Socket socket = TCPAdaptor.Socket();
                //socket.LingerState = new LingerOption(false, 0);
                try
                {
                    await socket.ConnectAsync(
                        new DnsEndPoint(hostname, int.Parse(port))
                    );
                    Log($"Connected!");
                    WriteLine("Connected!");
                }
                catch (Exception ex)
                {
                    Warning(ex.Message);
                    WriteLine();
                    WriteLine($"ERROR: {ex.Message}");
                    continue;
                }

                var stream = new NetworkStream(socket);
                Log($"Relaying Telnet to {hostname}:{port}");                
                await Task.WhenAny(
                    stream.CopyToAsync(Stream, cancel),
                    Stream.CopyToAsync(stream, cancel)
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return;
            }
        
        return;
        
        
    }
        
}

/*
public class RawSocketProtocol : Protocol
{
    public RetroNetTelnetProtocol(ILogger<RetroNetTelnetProtocol> logger) : base(logger)
    {
    }

    public override byte Command => 0xA6;

    protected override byte Version => 0x01;

    public override void OnListen()
    {

    }

    void Write(string message) => Send(NabuLib.FromASCII(message).ToArray());
    void WriteLine(string? message = null) => Write($"{message}\n");
    char[] Read() => Encoding.ASCII.GetChars(Recv(1));
    byte[] FromCharacters(params char[] chr) => Encoding.ASCII.GetBytes(chr);

    string ReadLine(bool echoOff = false, char? replacement = null)
    {
        var incoming = Array.Empty<char>();
        var line = string.Empty;
        while (true)
        {
            incoming = Read();
            if (incoming[0] == '\r') break;
            else if (incoming[0] is '\b')
            {
                if (!string.IsNullOrEmpty(line))
                    line = new(line.AsSpan()[..^1].ToArray());
                continue;
            }
            else
            {
                line += incoming[0];
                if (echoOff is false)
                {
                    if (replacement is not null) Send(FromCharacters(replacement.Value));
                    else Send((byte)incoming[0]);
                }
            }
        }
        return line;
    }

    string Prompt(string prompt, bool echoOff = false, char? replacement = null)
    {
        Write($"{prompt}: ");
        var input = ReadLine(echoOff, replacement);
        WriteLine();
        return input;
    }

    async Task Open(byte[] message)
    {
        using var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp
                );
        socket.NoDelay = true;
        socket.LingerState = new LingerOption(false, 0);

        var (next, i) = NabuLib.Slice(buffer, 0)


        try
        {
            Log($"Opening Socket to {hostname}:{port}...");
            await socket.ConnectAsync(
                new DnsEndPoint(hostname, int.Parse(port))
            );
            Log($"Connected!");
            WriteLine("Connected!");
        }
        catch (Exception ex)
        {
            Warning(ex.Message);
            WriteLine();
            WriteLine($"ERROR: {ex.Message}");
            continue;
        }

        var stream = new NetworkStream(socket);
        Log($"Relaying Telnet to {hostname}:{port}");
        await Task.WhenAny(
            stream.CopyToAsync(Stream, cancel),
            Stream.CopyToAsync(stream, cancel)
        );
        GC.Collect();
        return true;
    }

    public override async Task<bool> Handle(byte unhandled, CancellationToken cancel)
    {
        Log($"v{Version} Started.");

        /*
            [ FRAME ]
            [2      ]       Size     
            [   1   ]       Command
            [    XXX]       Message (Remainder)
            [  END  ]
        

        while (cancel.IsCancellationRequested is false)
            try
            {
                var (length, buffer) = ReadFrame();
                var (next, command)  = NabuLib.Pop(buffer, 0);
                var (_, message)     = NabuLib.Slice(buffer, next, length - 1); //Slice prevents overruns

                switch (command)
                {
                    case 0x00:
                        Warning($"v{Version} Received: 0, Aborting");
                        return false;
                    case 0xEF:
                        Log($"v{Version} Ending");
                        //Storage.End();
                        return false;
                    case 0x01:
                        await Open(message);
                        return true;
                    case 0x02:
                        Get(message);
                        return true;
                    case 0x03:
                        Put(message);
                        return true;
                    default:
                        Warning($"Unsupported: {Format(command)}");
                        return true;
                }

                
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return true;
            }

        return true;


    }

}*/