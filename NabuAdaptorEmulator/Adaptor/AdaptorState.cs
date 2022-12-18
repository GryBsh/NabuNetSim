using Nabu.Network;
using System;
using System.Text.RegularExpressions;

namespace Nabu.Adaptor;

public enum AdaptorMode
{
    LocalSerial = 0,
    RemoteSerial
}

public class AdaptorState
{

    public short Channel { get; set; }
    public bool ChannelKnown { get; set; }
    public string LastRate { get; set; } = "0.00";
    public Dictionary<string, byte[]> SegmentCache { get; } = new();
}