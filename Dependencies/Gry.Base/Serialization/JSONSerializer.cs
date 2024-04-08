using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;


namespace Gry.Serialization;

public class JSONSerializer : ISerialize
{

    public static string TypeName => "json";
    public static string[] ContentTypeNames { get; } = ["application/json", "text/json", "application/text+json"];

    public string Type => TypeName;
    public string[] ContentTypes => ContentTypeNames;

    JsonSerializer Serializer(ISerializerOptions options) 
        => JsonSerializer.Create(
            new JsonSerializerSettings
            {
                ContractResolver = options.LowerFirst ? new CamelCasePropertyNamesContractResolver() : null,
                Converters = new JsonConverter[] { new StringEnumConverter(), new ExpandoObjectConverter(), new KeyValuePairConverter() },
                Formatting = options.Compress ? Formatting.None : Formatting.Indented,
                MaxDepth = 100
            }
        );

    public TextReader Serialize<T>(ISerializerOptions options, params T[] documents)
    {
        JsonSerializer settings = Serializer(options); 
        var writer = new StringWriter();

        if (documents.Length is 1)
            settings.Serialize(writer, documents[0]);
        else
            settings.Serialize(writer, documents);

        var reader = new StringReader(writer.ToString());
        return reader;
    }

    public T[] Deserialize<T>(ISerializerOptions options, TextReader source)
    {
        var json = JToken.ReadFrom(new JsonTextReader(source));
        if (json.Type is JTokenType.Array)
        {
            return json.ToObject<T[]>() ?? [];
        }
        else
        {
            var value = json.ToObject<T>(Serializer(options));
            if (value is not null)
                return [value];
            else
                return [];
        }

    }
}
