using Nabu.Network;
using System;
using System.Text.RegularExpressions;

namespace Nabu.Adaptor;

public class AdaptorState
{

    public short Channel { get; set; }
    public bool ChannelKnown => Channel is > 0 and < 0x100;
    public string LastRate { get; set; } = "0.00";
    public Dictionary<string, byte[]> SegmentCache { get; } = new();

}