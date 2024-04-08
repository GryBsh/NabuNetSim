using Lgc;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace Gry.Adapters;



public class AdapterManager : ISingletonDependency   
{
    ConcurrentDictionary<AdapterDefinition, Adapter> Adapters { get; } = [];

    public IEnumerable<AdapterDefinition> Defined => Adapters.Keys;
    public IEnumerable<Adapter> Running => Adapters.Values;


    public AdapterState GetState(AdapterDefinition definition)
        => this[definition]?.State ?? AdapterState.Stopped;


    public Adapter? this[AdapterDefinition definition] 
    {
        get
        {
            if (Adapters.TryGetValue(definition, out Adapter? adapter)) 
                return adapter;
            return null;
        }
    }

    public void Add(AdapterDefinition definition, Adapter adapter)
    {
        Adapters.AddOrUpdate(definition, adapter,(o,a) => adapter);
    }

    public void Remove(AdapterDefinition definition) 
    {  
        if (Adapters.TryGetValue(definition, out Adapter? adapter))
        {
            adapter.Dispose();
            Adapters.Remove(definition, out _);
        }
    }

    public void Cancel()
    {
        foreach (var (_, adapter) in Adapters)
            adapter.Cancel();
    }
}
