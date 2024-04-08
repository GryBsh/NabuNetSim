namespace Gry.Serialization;


public interface ISerializeProvider 
{
    ISerialize? For(string type);
    TextReader? Serialize<T>(string type, ISerializerOptions options, params T[] documents);
    T[]? Deserialize<T>(string type, ISerializerOptions options, TextReader source);

}
