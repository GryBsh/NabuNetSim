using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Nabu.Binary;

public class SerialAdapter : BinaryAdapter
{
    SerialPort? Serial;
    readonly AdaptorSettings Settings;
    public SerialAdapter(
        AdaptorSettings settings,
        ILogger logger
    ) : base(logger)
    {
        Settings = settings;
    }
    public override bool Connected => Serial?.IsOpen is true;
    public override void Open()
    {
        if (Serial is not null && Serial.IsOpen) { return; }
        else if (Serial is not null) {
            Serial?.Dispose();
            Serial = null;
        }

        Serial = new(
            Settings.Port,
            Settings.BaudRate,
            Parity.None,
            8,
            StopBits.Two
        ){
            Handshake = Handshake.None,
            RtsEnable = true,
            DtrEnable = true,
            ReadTimeout = 60000,
        };
        Serial.Open();
        if (Serial.IsOpen)
            Stream = Serial.BaseStream;
        else
            throw new Exception($"Failed open {Settings.Port}");

        Logger.LogInformation($"Listening on {Settings.Port}");
    }

    public override void Close()
    {
        Serial?.Close();
        Serial?.Dispose();
        Serial = null;
    }
}
