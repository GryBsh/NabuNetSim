
namespace Gry.Serialization
{
    public interface ISerializer
    {
        T[]? Deserialize<T>(string type, ISerializerOptions options, TextReader source);
        TextReader? Serialize<T>(string type, ISerializerOptions options, params T[] documents);
    }
}