using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Serialization;

namespace Nabu;

public abstract record ReactiveRecord :
    INotifyPropertyChanged,
    INotifyPropertyChanging,
    IObservable<INotifyPropertyChanged>,
    IObservable<INotifyPropertyChanging>
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    IObservable<INotifyPropertyChanged> ObservableChanged { get; }
    IObservable<INotifyPropertyChanging> ObservableChanging { get; }

    public ReactiveRecord()
    {
        ObservableChanged = Observable.FromEvent<PropertyChangedEventHandler, INotifyPropertyChanged>(
                        h => PropertyChanged += h,
                        r => PropertyChanged -= r
                    );
        ObservableChanging = Observable.FromEvent<PropertyChangingEventHandler, INotifyPropertyChanging>(
                        h => PropertyChanging += h,
                        r => PropertyChanging -= r
                    );
    }

    protected void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected void NotifyPropertyChanging(string propertyName)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    public IDisposable Subscribe(IObserver<INotifyPropertyChanged> observer)
    {
        return ObservableChanged.Subscribe(observer);
    }

    public IDisposable Subscribe(IObserver<INotifyPropertyChanging> observer)
    {
        return ObservableChanging.Subscribe(observer);
    }
}

public abstract record DeserializedObject : ReactiveRecord
{
    [YamlIgnore()]
    [JsonExtensionData()]
    protected DeserializedDictionary<object?> Properties { get; } = new();
    public DeserializedObject() { }

    public object? this[string name]
    {
        get => Get<object>(name);
        set => Set(name, value);
    }

    protected T? Get<T>(string key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        var canGet = Properties.TryGetValue(key, out object? value);
        if (!canGet) return default;

        return FromValue<T>(value);
    }

    protected void Set<T>(string key, T? value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        NotifyPropertyChanging(key);
        _ = Properties.AddOrUpdate(
                key,
                value,
                (k, o) => value
            );
        NotifyPropertyChanged(key);
    }

    protected virtual object? MapValue(object? value)
    {
        return value;
    }

    static (bool, Func<string, T?>) CanParse<T>(string? value)
    {
        try
        {
            var parseMethod = typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new[] { typeof(string) });
            if (parseMethod is null) return default;
            return (true, (val) => (T?)parseMethod.Invoke(null, new[] { val }));
        }
        catch
        {
            return (false, (v) => default);
        }
    }

    protected T? FromValue<T>(object? value)
    {
        value = MapValue(value);
        value = value switch
        {
            null => default,
            T desired => desired,
            JToken token => token.ToObject<T>(),
            JsonElement element => element.Deserialize<T>(),
            JsonNode node => node.Deserialize<T>(),
            IDictionary<string, object?> dict and not DeserializedDictionary<object?> => new DeserializedDictionary<object?>(dict),
            string e when typeof(T).IsEnum && Enum.TryParse(typeof(T), e, true, out var p) => (T?)p,
            string parsed when CanParse<T>(parsed) is var (canParse, parser) && canParse => parser.Invoke(parsed),
            _ => default
        };

        return (T?)value;
    }
}