namespace Nabu.Adaptor;

public record AdaptorSettings()
{
    public bool ChannelPrompt { get; set; } = false;
    public short Channel { get; set; } = 0x0000;
}


