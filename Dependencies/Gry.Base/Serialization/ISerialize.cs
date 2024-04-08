namespace Gry.Serialization;



public interface ISerialize
{
    string Type { get; }
    string[] ContentTypes { get; }

    TextReader Serialize<T>(ISerializerOptions options, params T[] documents);
    T[] Deserialize<T>(ISerializerOptions options, TextReader source);
}
