namespace Gry.Serialization;

public class SerializerProvider(IEnumerable<ISerialize> serializers) : ISerializeProvider, ISerializer
{
    public ISerialize? For(string type) => serializers.FirstOrDefault(s => s.Type == type || s.ContentTypes.Contains(type));

    public TextReader? Serialize<T>(string type, ISerializerOptions options, params T[] documents)
    {
        return For(type)?.Serialize(options, documents);
    }

    public T[]? Deserialize<T>(string type, ISerializerOptions options, TextReader source)
    {
        return For(type)?.Deserialize<T>(options, source);
    }
}
