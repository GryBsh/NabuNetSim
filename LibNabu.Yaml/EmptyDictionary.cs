using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Nabu;

public class EmptyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
    TKey[] EmptyKeys { get; } = Array.Empty<TKey>();
    TValue[] EmptyValues { get; } = Array.Empty<TValue>();
    IEnumerable<KeyValuePair<TKey, TValue>> EmptyPairs { get; } = Array.Empty<KeyValuePair<TKey, TValue>>();

    public TValue this[TKey key]
    {
        get => default;
        set { }
    }

    public ICollection<TKey> Keys => EmptyKeys;

    public ICollection<TValue> Values => EmptyValues;

    public int Count { get; } = 0;

    public bool IsReadOnly { get; } = true;

    public void Add(TKey key, TValue value)
    {

    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {

    }

    public void Clear()
    {

    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return false;
    }

    public bool ContainsKey(TKey key)
    {
        return false;
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {

    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return EmptyPairs.GetEnumerator();
    }

    public bool Remove(TKey key)
    {
        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return false;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        value = default;
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return EmptyPairs.GetEnumerator();
    }
}

