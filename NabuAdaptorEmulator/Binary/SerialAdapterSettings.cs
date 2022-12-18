using System.IO.Ports;

namespace Nabu.Binary;

public record SerialAdapterSettings()
{
    public string Port { get; set; } = SerialPort.GetPortNames()[0];
    public int BaudRate { get; set; } = 111865;
    public Parity Partity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.Two;
}
