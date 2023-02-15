using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Messages;
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


namespace Nabu.Network.RetroNet;


public class RetroNetTelnetProtocol : Protocol
{
    public RetroNetTelnetProtocol(IConsole<RetroNetTelnetProtocol> logger) : base(logger)
    {
    }

    public override byte[] Commands { get; } = new byte[] { 0xA6 };

    public override byte Version => 0x01;

    void Write(string message) => Send(NabuLib.FromASCII(message).ToArray());
    void WriteLine(string? message = null) => Write($"{message}\n");
    char[] Read() => Encoding.ASCII.GetChars(Recv(1));
    byte[] FromCharacters(params char[] chr) => Encoding.ASCII.GetBytes(chr);
    string ReadLine(bool echoOff = false, char? replacement = null)
    {
        var line = string.Empty;
        while (true)
        {
            var incoming = Read();
            if (incoming[0] == '\r') break;
            else if (incoming[0] is '\b')
            {
                if (!string.IsNullOrEmpty(line))
                {
                    line = new(line.AsSpan()[..^1].ToArray());
                    Send((byte)incoming[0]);
                }
                continue;
            }
            else
            {
                line += incoming[0];
                if (echoOff is false)
                {
                    if (replacement is not null)
                        Send(FromCharacters(replacement.Value));
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

    CancellationTokenSource Cancel { get; set; } = new CancellationTokenSource();
    Timer? StartupDetection { get; set; }

    public override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        WriteLine("Socialist Workers Terminal vL.T.S");
        WriteLine("\"It works for us now comrade!\"");
        WriteLine();
        while (cancel.IsCancellationRequested is false)
            try
            {
                var hostname = Prompt("Hostname");
                var port = Prompt("Port");
                WriteLine($"Capitalist Swine Detected, engage protocols...");
                Log($"Opening Socket to {hostname}:{port}...");
                WriteLine($"Connecting to {hostname}:{port}");

                using Socket socket = NabuLib.Socket();

                if (int.TryParse(port, out var portNumber))
                {
                    await socket.ConnectAsync(
                        new DnsEndPoint(hostname, portNumber)
                    );
                    Log($"Connected!");
                    WriteLine("Connected!");
                }
                else
                {
                    WriteLine($"ERROR: bad port");
                    continue;
                }

                
                Log($"Relaying Telnet to {hostname}:{port}");

                Cancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, CancellationToken.None);
                var remote = new NetworkStream(socket);
                var nabu = Stream;
                
                try
                {
                    await Task.WhenAny(
                        nabu.CopyToAsync(remote, Cancel.Token),
                        remote.CopyToAsync(nabu, Cancel.Token)
                    );
                }
                catch
                {
                    Cancel.Cancel();
                    return;
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return;
            }
       
        return;


    }

    public override void Reset()
    {
        Cancel?.Cancel();

    }
}

