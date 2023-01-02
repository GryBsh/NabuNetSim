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
using System.Threading.Tasks;


namespace Nabu.Network;

[Flags]
public enum SocketOptions
{
    None = 0b_0000_0000_0000_0000
}

public class RetroNetTelnetProtocol : Protocol
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
    
    public override async Task<bool> Handle(byte unhandled, CancellationToken cancel)
    {
        Log($"v{Version} Started.");

        while (cancel.IsCancellationRequested is false)
            try
            {

                WriteLine("Socialist Workers Terminal vL.T.S");
                WriteLine("It works for us now comrad!");
                WriteLine();
                var hostname = Prompt("Hostname");
                var port = Prompt("Port");
                //WriteLine($"{hostname} is for capitalist swine! But very well.");
                //var username = Prompt("Username");
                //var password = Prompt("Password", replacement: '*');
                //using var client = new PrimS.Telnet.Client(hostname, int.Parse(port), cancel);
                //var loggedIn = await client.TryLoginAsync(username, password, 10000, "\r\n");

                using var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp
                );
                socket.NoDelay = true;
                socket.LingerState = new LingerOption(false, 0);

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
                                
                await Task.WhenAny(
                    stream.CopyToAsync(Stream, cancel),
                    Stream.CopyToAsync(stream, cancel)
                );

                return true;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return true;
            }
        
        return true;
        
        
    }
        
}
