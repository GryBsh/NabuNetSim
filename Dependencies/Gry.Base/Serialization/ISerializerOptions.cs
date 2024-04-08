namespace Gry.Serialization;

public interface ISerializerOptions
{
    bool Compress { get; }
    bool LowerFirst { get; }
}
