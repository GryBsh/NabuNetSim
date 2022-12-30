using System;
using System.Text.RegularExpressions;

namespace Nabu.Network;

public class NabuNetAdaptorState
{

    public short Channel { get; set; }
    public bool ChannelKnown => Channel is > 0 and < 0x100;

}