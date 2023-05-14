using Nabu.Adaptor;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels;

public class StatusViewModel : ReactiveObject
{
    public StatusViewModel(MenuViewModel menu, Settings settings)
    {
        //Home = home;
        Menu = menu;
        Settings = settings;
        Observable
            .Interval(TimeSpan.FromSeconds(5))
            .Subscribe(_ => {
                this.RaisePropertyChanged(nameof(Serial));
                this.RaisePropertyChanged(nameof(TCP));
                this.RaisePropertyChanged(nameof(Connections));
                //NotifyChange();
            }
        );
    }

    //public HomeViewModel Home { get; }
    public MenuViewModel Menu { get; }
    //public LogViewModel? Log => Home.Log;
    public Settings Settings { get; }

    void NotifyChange()
    {
        //Home.RaisePropertyChanged(nameof(Home.Status));
    }

    public ICollection<SerialAdaptorSettings> Serial
    {
        get => Settings.Adaptors.Serial;
    }

    public ICollection<TCPAdaptorSettings> TCP
    {
        get => Settings.Adaptors.TCP;
    }

    public ICollection<TCPAdaptorSettings> Connections
    {
        get => TCPAdaptor.Connections.Values.ToList();
    }

    public string AdaptorStatus(AdaptorSettings settings)
    {
        return settings.State switch
        {
            ServiceShould.Run => "Running",
            ServiceShould.Restart => "Stopping",
            ServiceShould.Stop => "Stopped",
            _ => "Unknown"
        };
    }

}
