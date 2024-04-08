using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Gry;


public static class ObservableExtensions
{
    public static void SetItems<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }
}


