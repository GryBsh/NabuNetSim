using Gry;
using NHACP.Messages;
using NHACP.Messages.V1;
using NHACP.V01;
using System.Net.Sockets;


namespace NHACP;
public class NHACPClient
{
    readonly TcpClient client;
    readonly BinaryReader reader;
    readonly BinaryWriter writer;

    NHACPV1SessionStarted? Session { get; set; }
    bool CRCEnabled => Session is not null && Session.Flags.HasFlag(NHACPOptions.CRC8) is true;

    public NHACPClient(string hostname, int port)
    {
        client = new(hostname, port);
        var stream = client.GetStream();
        reader = new BinaryReader(stream);
        writer = new BinaryWriter(stream);
    }

    public NHACPResponse Hello(ushort version = 1, NHACPOptions flags = NHACPOptions.None, NHACPV1SessionId sessionId = NHACPV1SessionId.User)
    {
        Send(new NHACPV1Hello(flags, version, sessionId));

        var response = Receive();

        if (response.IsError)
            return new NHACPV1Error(response);

        return Session = new NHACPV1SessionStarted(flags, response);
    }

    public NHACPV1DateTime DateTime(byte sessionId)
    {
        Send(new(sessionId, 0x04));
        var response = Receive();
        return new(response);
    }

    void Send(NHACPRequest message)
    {
        byte[] crc = CRCEnabled ? [CRC.GenerateCRC8(message.Body)] : [];

        var length = Bytes.FromUShort((ushort)(1 + message.Body.Length));
        writer.Write([
            0x8F,
            message.SessionId,
            ..length,
            message.Type,
            ..message.Body.Span,
            ..crc
        ]);
    }
    NHACPResponse Receive()
    {
        var length = Bytes.ToUShort(reader.ReadBytes(2));
        var data = reader.ReadBytes(length);

        if (CRCEnabled)
        {
            var expectedCRC = data[^1];
            data = data[..^1];
            var payloadCRC = CRC.GenerateCRC8(data);

            if (expectedCRC != payloadCRC)
                throw new InvalidOperationException(
                    $"CRC mismatch, {Bytes.Format(expectedCRC)} != {Bytes.Format(payloadCRC)}"
                );
        }
        return new NHACPResponse(data);
    }
}

