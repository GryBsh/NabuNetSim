using Gry.Conversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Serialization;
using TextJson = System.Text.Json.Serialization;
namespace Gry;

public interface INullType { }


public partial record Model : 
    INotifyPropertyChanging, 
    INotifyPropertyChanged,
    IObservable<PropertyChangingEventArgs>,
    IObservable<PropertyChangedEventArgs>
{

    public Model(bool enableEvents = true) 
    {
        if (enableEvents is false)
        {
            DisableNotifications = true;
            return;
        }

        Changing = Observable.FromEvent<PropertyChangingEventHandler, PropertyChangingEventArgs>(
            add => PropertyChanging += add, 
            remove => PropertyChanging -= remove
        );

        Changed = Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
            add => PropertyChanged += add,
            remove => PropertyChanged -= remove
        );
    }

    ConcurrentDictionary<string, bool> HasChanged { get; } = new();

    

    [YamlIgnore()]
    [JsonExtensionData(WriteData = false)]
    [TextJson.JsonExtensionData()]
    protected DataDictionary Properties { get; private init; } = new();

    public object? this[string name]
    {
        get => Get<object>(name);
        set => Set(name, value);
    }

    /// <summary>
    ///  Gets the value of the property with the specified name.
    /// </summary>
    /// <typeparam name="T">The values type</typeparam>
    /// <param name="key">The name of the property</param>
    /// <returns>The value or default</returns>
    /// <exception cref="ArgumentNullException">
    /// The key is null.
    /// </exception>
    protected T? Get<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var canGet = Properties.TryGetValue(key, out object? value);
        if (!canGet) return default;

        return Convert<T>(value, TryConvert<T>);
    }

    protected void SetIfDefault<T>(string key, T? value)
    {
        var current = Get<T>(key);
        if (current is null || current.Equals(default(T)) is true)
            Set(key, value);
    }

    protected void Set<T>(string key, T? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        NotifyPropertyChanging(key);
        _ = Properties.AddOrUpdate(
                key,
                value,
                (k, o) => value
            );
        HasChanged[key] = true;
        NotifyPropertyChanged(key);
        
    }

    public IEnumerable<string> Keys => Properties.Keys;

    /// <summary>
    ///     When overridden in a derived class, converts the <paramref name="value"/> .
    /// </summary>
    /// <typeparam name="T">The desired return type</typeparam>
    /// <param name="value">The value to convert</param>
    /// <returns></returns>
    protected virtual (bool, object?) TryConvert<T>(object? value)
    {
        return (value is T?, value); ;
    }

    public static T? Convert<T>(object? value, Func<object?,(bool, object?)> converter)
    {
        if (value is null) return default;

        (var valid, value) = converter(value);
        if (valid) return (T?)value;

        var rType = typeof(T);
        var vType = value?.GetType();
        object? result = value switch
        {
            T desired => desired,
            //_ when vType?.IsAssignableTo(rType) is true => value,
            JToken token => token.ToObject<T>(),
            JsonNode node => node.Deserialize<T>(),
            IDictionary<string, object?> dict and not DataDictionary<object?> when rType.IsAssignableTo(typeof(DataDictionary<object?>))
                => new DataDictionary<object?>(dict),
            string e when rType.IsEnum && Enum.TryParse(rType, e, true, out var p) => p,
            string parsed when ParseMethodConverter<T>.CanParse(parsed) is var (canParse, parser) && canParse => parser(parsed),
            _ => default
        };

        return (T?)result;
    }

    [YamlIgnore]
    [JsonIgnore]
    [TextJson.JsonIgnore]
    public bool DisableNotifications { get; set; } = false;

    #region INotifyPropertyChang(ing/ed)

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    protected void NotifyPropertyChanged(string propertyName)
    {
        if (DisableNotifications) return;
        var context = new PropertyChangedEventArgs(propertyName);
        Task.Run(() => PropertyChanged?.Invoke(this, context));
    }
    protected void NotifyPropertyChanging(string propertyName)
    {
        if (DisableNotifications) return;
        var context = new PropertyChangingEventArgs(propertyName);
        Task.Run(() => PropertyChanging?.Invoke(this, context));
    }

    #endregion

    #region Observables

    IObservable<PropertyChangingEventArgs> Changing { get; }
    IObservable<PropertyChangedEventArgs> Changed { get; }

    public IDisposable Subscribe(IObserver<PropertyChangingEventArgs> observer)
    {
        if (Changing is null)
            return Disposable.Empty;
        return Changing.Subscribe(observer);
    }

    public IDisposable Subscribe(IObserver<PropertyChangedEventArgs> observer)
    {
        if (Changed is null)
            return Disposable.Empty;
        return Changed.Subscribe(observer);
    }

    #endregion

    public static T Create<T>(IDictionary<string, object?> dictionary) where T : Model, new()
    {
        return new T() { Properties = new(dictionary) };
    }

    /// <summary>
    /// Wraps the values of a DataObject in another compatible DataObject
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static TResult As<TSource, TResult>(TSource data)
        where TSource : Model
        where TResult : Model, TSource, new()
    {
        var r = new TResult();
        //Apply default values to any keys without values
        Apply(r.Properties, data.Properties);
        return new TResult() { Properties = data.Properties };
    }

    protected static void Apply(DataDictionary from, DataDictionary to, bool overwrite = false)
    {
        foreach (var (k, v) in from)
        {
            if (overwrite is false && to.ContainsKey(k))   
                continue; //Keep Existing Value
            to[k] = v;
        }
    }

    /// <summary>
    /// Copies the contents of one <see cref="Model"/> to another
    /// </summary>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="data">The source <see cref="Model"/></param>
    /// <returns>The desired result typed object</returns>
    public static TResult Copy<TSource, TResult>(TSource data)
        where TSource : Model
        where TResult : Model, TSource, new()
    {
        var r = new TResult();
        Apply(data.Properties, r.Properties, true);
        return r;
    }

}




