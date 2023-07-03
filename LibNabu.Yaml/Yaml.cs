using Nabu.Network;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Nabu;

public static class Yaml
{
    public static async Task<T[]> Deserialize<T>(string medium, IFileCache? cache = null, IHttpCache? http = null)
    {
        var file = http switch
        {
            not null when NabuLib.IsHttp(medium) => await http.GetBytes(medium),
            //_ when Path.Exists(medium) && cache is not null => await cache.GetBytes(medium),
            _ when Path.Exists(medium) => await File.ReadAllBytesAsync(medium),
            _ => Encoding.Default.GetBytes(medium),
        };

        using TextReader source = new StreamReader(new MemoryStream(file.ToArray()));

        return Deserialize<T>(source);
    }

    public static T[] Deserialize<T>(TextReader source)
    {
        var naming = new LowerFirstNamingConvention();
        var reader = new Parser(source);
        var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(naming)
                               .Build();

        reader.Expect<StreamStart>();

        var documents = new List<IDictionary<string, object>>();

        while (reader.Accept<DocumentStart>())
        {
            var document = deserializer.Deserialize<IDictionary<string, object>>(reader);
            documents.Add(document);
        }

        using var writer = new StringWriter();
        var serializer = new SerializerBuilder()
                            .JsonCompatible()
                            .Build();

        serializer.Serialize(writer, documents.ToArray());
        return JArray.Parse(writer.ToString())?.ToObject<T[]>() ??
               Array.Empty<T>();
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

    public class LowerFirstNamingConvention : INamingConvention
    {
        private static readonly TextInfo Text = CultureInfo.InvariantCulture.TextInfo;

        public string Apply(string value)
            => Text.ToLower(value[0]) + value[1..];
    }
}