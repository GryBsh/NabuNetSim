namespace Gry.Serialization;

public record SerializerOptions : ISerializerOptions
{
    public bool Compress { get; set; } = true;
    public bool LowerFirst { get; set; } = true;
}
