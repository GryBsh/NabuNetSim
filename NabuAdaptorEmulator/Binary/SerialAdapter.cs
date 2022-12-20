using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Nabu.Binary;

public class SerialAdapter : BinaryAdapter
{
    SerialPort? Serial;
    readonly SerialAdapterSettings Settings;
    public SerialAdapter(
        SerialAdapterSettings settings,
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
            Settings.Partity,
            Settings.DataBits,
            Settings.StopBits
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
    }
}
