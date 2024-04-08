using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Gry.Serialization;

public class YAMLSerializer : ISerialize
{
    public static string TypeName => "yaml";
    public static string[] ContentTypeNames => ["application/yaml", "text/yaml"];

    static IDeserializer Deserializer(ISerializerOptions options)
    {
        var builder = new DeserializerBuilder();
        if (options.LowerFirst) builder.WithNamingConvention(new LowerFirstNamingConvention());
        return builder.Build();
    }

    static YamlDotNet.Serialization.ISerializer JsonSerializer(ISerializerOptions options)
    {
        var builder = new SerializerBuilder().JsonCompatible();
        SerializerOptions(builder, options);
        return builder.Build();
    }

    static YamlDotNet.Serialization.ISerializer Serializer(ISerializerOptions options)
    {
        var builder = new SerializerBuilder();
        SerializerOptions(builder, options);
        return builder.Build();
    }

    static void SerializerOptions(SerializerBuilder builder, ISerializerOptions options)
    {
        if (options.LowerFirst) builder.WithNamingConvention(new LowerFirstNamingConvention());
    }

    public string Type => TypeName;
    public string[] ContentTypes => ContentTypeNames;

    public T[] Deserialize<T>(ISerializerOptions options, TextReader source)
    {

        var reader = new Parser(source);
        var deserializer = Deserializer(options);

        reader.Consume<StreamStart>();

        var documents = new List<IDictionary<string, object>>();

        while (reader.TryConsume<DocumentStart>(out _))
        {
            var document = deserializer.Deserialize<IDictionary<string, object>>(reader);
            documents.Add(document);
        }

        using var writer = new StringWriter();
        var serializer = JsonSerializer(options);

        serializer.Serialize(writer, documents.ToArray());
        return JArray.Parse(writer.ToString())?.ToObject<T[]>() ?? [];
    }

    public TextReader Serialize<T>(ISerializerOptions options, params T[] documents)
    {

        var serializer = Serializer(options);
        var strings = from document in documents
                      where document is not null
                      select serializer.Serialize(document);

        return new StringReader(string.Join("---\n", strings));
    }

    public class LowerFirstNamingConvention : INamingConvention
    {
        private static readonly TextInfo Text = CultureInfo.InvariantCulture.TextInfo;

        public string Apply(string value)
            => Text.ToLower(value[0]) + value[1..];

        public string Reverse(string value)
        {
            return string.Concat(Apply(value).Reverse());
        }
    }
}
