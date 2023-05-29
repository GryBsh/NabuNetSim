namespace Nabu;

public interface IWithOptions
{
    IDictionary<string, object?>? Options { get; set; }
}

public record DeserializedWithOptions : DeserializedObject
{
    public IDictionary<string, object?>? Options
    {
        get => Get<IDictionary<string, object?>>(nameof(Options));
        set => Set(nameof(Options), value);
    }

    public T? Option<T>(string name)
    {
        if (Options?.TryGetValue(name, out var value) is true)
        {
            return FromValue<T>(value);
        }
        return default;
    }

}

