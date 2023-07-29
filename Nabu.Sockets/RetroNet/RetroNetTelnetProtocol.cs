using Nabu.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Nabu.Network.RetroNet;

public class RetroNetTelnetProtocol : Protocol
{
    public RetroNetTelnetProtocol(ILog<RetroNetTelnetProtocol> logger) : base(logger)
    {
    }

    public override byte[] Commands { get; } = new byte[] { 0xA6 };

    public override byte Version => 0x01;

    private void Write(string message) => Write(NabuLib.FromASCII(message).ToArray());

    private void WriteLine(string? message = null) => Write($"{message}\n");

    private char[] Read() => Encoding.ASCII.GetChars(Read(1));

    private byte[] FromCharacters(params char[] chr) => Encoding.ASCII.GetBytes(chr);

    private string ReadLine(bool echoOff = false, char? replacement = null)
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
                    Write((byte)incoming[0]);
                }
                continue;
            }
            else
            {
                line += incoming[0];
                if (echoOff is false)
                {
                    if (replacement is not null)
                        Write(FromCharacters(replacement.Value));
                    else Write((byte)incoming[0]);
                }
            }
        }
        return line;
    }

    private string Prompt(string prompt, bool echoOff = false, char? replacement = null)
    {
        Write($"{prompt}: ");
        var input = ReadLine(echoOff, replacement);
        WriteLine();
        return input;
    }

    //CancellationTokenSource Cancel { get; set; } = new CancellationTokenSource();
    private Timer? StartupDetection { get; set; }

    protected override async Task Handle(byte unhandled, CancellationToken cancel)
    {
        WriteLine("Socialist Workers Terminal vL.T.S");
        WriteLine("\"It works for us now comrade!\"");
        WriteLine();
        var cancelLoop = CancellationTokenSource.CreateLinkedTokenSource(cancel, CancellationToken.None);
        while (cancelLoop.IsCancellationRequested is false)
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

                var remote = new NetworkStream(socket);
                var nabu = Stream;

                try
                {
                    while (cancelLoop.IsCancellationRequested is false)
                    {
                        if (remote!.DataAvailable)
                        {
                            var buffer = new Memory<byte>();
                            var received = await remote!.ReadAtLeastAsync(buffer, 1, true, cancel);
                            await nabu.WriteAsync(buffer, cancel);
                        }

                        var dataAvailable = nabu switch
                        {
                            NetworkStream ns => ns.DataAvailable,
                            _ => nabu.Length > 0
                        };

                        if (dataAvailable)
                        {
                            var buffer = new Memory<byte>();
                            var received = await remote!.ReadAtLeastAsync(buffer, 1, true, cancel);
                            await remote.WriteAsync(buffer, cancel);
                        }
                    }
                }
                catch
                {
                    cancelLoop.Cancel();
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

    /*
    public override void Reset()
    {
        Cancel?.Cancel();
    }
    */
}