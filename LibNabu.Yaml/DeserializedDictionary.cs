using System.Collections.Concurrent;

namespace Nabu;

public class DeserializedDictionary<T> : ConcurrentDictionary<string, T>
{
    public DeserializedDictionary() : base(StringComparer.InvariantCultureIgnoreCase) { }
    public DeserializedDictionary(IDictionary<string, T> dictionary) : 
        base(dictionary ?? new Dictionary<string, T>(), StringComparer.InvariantCultureIgnoreCase) { }
}
