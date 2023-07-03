using Nabu;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;

namespace Napa;

public class ObservableRange<T> : ObservableCollection<T>
{
    public ObservableRange()
    {
    }

    public bool Muted { get; private set; } = false;

    public void Mute() => Muted = true;

    public void SetRange(IEnumerable<T> items)
    {
        //Mute();
        ClearItems();
        foreach (var item in items)
        {
            Add(item);
        }
        //Unmute();
        //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList()));
    }

    public void Unmute() => Muted = false;

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!Muted) base.OnCollectionChanged(e);
    }
}