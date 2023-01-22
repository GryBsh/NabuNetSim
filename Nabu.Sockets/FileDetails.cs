namespace Nabu.Network;

public record FileDetails
{
    public int FileSize { get; set; }

    public short CreatedYear { get; set; }
    public byte CreatedMonth { get; set; }
    public byte CreatedDay { get; set; }
    public byte CreatedHour { get; set; }
    public byte CreatedMinute { get; set; }
    public byte CreatedSecond { get; set; }

    public short ModifiedYear { get; set; }
    public byte ModifiedMonth { get; set; }
    public byte ModifiedDay { get; set; }
    public byte ModifiedHour { get; set; }
    public byte ModifiedMinute { get; set; }
    public byte ModifiedSecond { get; set; }

    public string? Filename { get; set; }
    public bool IsFile { get; set; }
    public bool Exists { get; set; }
}

