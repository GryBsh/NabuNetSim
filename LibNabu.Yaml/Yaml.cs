using Newtonsoft.Json.Linq;
using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using System.Globalization;
using Nabu.Network;

namespace Nabu;

public static class Yaml
{
    public class LowerFirstNamingConvention : INamingConvention
    {
        static readonly TextInfo Text = CultureInfo.InvariantCulture.TextInfo;
        public string Apply(string value)
            => Text.ToLower(value[0]) + value[1..];
    }

    public static string Serialize<T>(params T[] documents)
    {
        var naming = new LowerFirstNamingConvention();
        var serializer = new SerializerBuilder()
                            .WithNamingConvention(naming)
                            .Build();

        var strings = from document in documents
                      where document is not null
                      select serializer.Serialize(document);

        return string.Join("---\n", strings);
    }

    public static async Task<T[]> Deserialize<T>(string medium, IFileCache cache, IHttpCache? http = null)
    {
        var file = http switch
        {
            not null when NabuLib.IsHttp(medium) => await http.GetBytes(medium),
            null when Path.Exists(medium) => await cache.GetFile(medium),
            _ => Memory<byte>.Empty
        };

        using TextReader source =
            file.Equals(Memory<byte>.Empty) ?
                new StringReader(medium) :
                new StreamReader(new MemoryStream(file.ToArray()));

        return Deserialize<T>(source);
    }

    public static T[] Deserialize<T>(TextReader source)
    {
        var naming = new LowerFirstNamingConvention();
        var reader = new Parser(source);
        var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(naming)
                               .Build();

        _ = reader.TryConsume<StreamStart>(out var _);

        var documents = new List<IDictionary<string, object>>();

        while (reader.TryConsume<DocumentStart>(out var _))
        {
            reader.Accept<NodeEvent>(out var nextNode); //Peek next node
            switch (nextNode)
            {
                case SequenceStart seq:
                    var elements = deserializer.Deserialize<IDictionary<string, object>[]>(reader);
                    documents.AddRange(elements);
                    break;
                default:
                    var document = deserializer.Deserialize<IDictionary<string, object>>(reader);
                    documents.Add(document);
                    break;
            }
        }

        using var writer = new StringWriter();
        var serializer = new SerializerBuilder()
                            .JsonCompatible()
                            .Build();

        serializer.Serialize(writer, documents.ToArray());
        return JArray.Parse(writer.ToString())?.ToObject<T[]>() ??
               Array.Empty<T>();
    }
}
