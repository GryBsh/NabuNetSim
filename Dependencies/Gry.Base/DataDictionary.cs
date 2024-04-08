using System.Collections.Concurrent;

namespace Gry;

/// <summary>
///     A concurrent dictionary that requires a key comparer.
/// </summary>
/// <typeparam name="TKey">
///     The type of the keys in the dictionary.
/// </typeparam>
/// <typeparam name="TValue">
///     The type of the values in the dictionary.
/// </typeparam>
public class DataDictionary<TKey,TValue> : ConcurrentDictionary<TKey, TValue> where TKey : notnull
{
    public DataDictionary(IEqualityComparer<TKey> keyComparer) : base(keyComparer)
    {
    }

    public DataDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> keyComparer) :
        base(dictionary, keyComparer)
    { }

}

/// <summary>
///    A concurrent dictionary with string keys that ignores case.
/// </summary>
/// <typeparam name="T">
///     The type of the values in the dictionary.
/// </typeparam>
public class DataDictionary<T> : DataDictionary<string, T>
{
    static readonly StringComparer _comparer = StringComparer.InvariantCultureIgnoreCase;

    public DataDictionary() : base(_comparer)
    {
    }

    public DataDictionary(IDictionary<string, T> dictionary) :
        base(dictionary, _comparer)
    { }

}

public class DataDictionary : DataDictionary<object?>
{
    public DataDictionary() : base()
    {
    }

    public DataDictionary(IDictionary<string, object?> dictionary) :
        base(dictionary)
    { }
}

