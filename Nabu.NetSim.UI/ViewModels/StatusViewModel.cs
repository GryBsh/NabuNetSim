using Nabu.Adaptors;
using Nabu.Settings;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels;

public class StatusViewModel : ReactiveObject, IActivatableViewModel
{
    public StatusViewModel(
        HomeViewModel home,
        AdaptorSettingsViewModel menu,
        LogViewModel logViewer,
        GlobalSettings settings
    )
    {
        Home = home;
        Menu = menu;
        Settings = settings;
        Activator = new();
        LogViewer = logViewer;

        this.WhenActivated(
            disposables =>
            {
                Observable
                    .Interval(TimeSpan.FromSeconds(30), RxApp.TaskpoolScheduler)
                        .Subscribe(_ =>
                        {
                            this.RaisePropertyChanged(nameof(Serial));
                            this.RaisePropertyChanged(nameof(TCP));
                            this.RaisePropertyChanged(nameof(Connections));
                            //NotifyChange();
                        }
                    ).DisposeWith(disposables);
            }
        );
    }

    public HomeViewModel Home { get; }
    public AdaptorSettingsViewModel Menu { get; }
    public LogViewModel LogViewer { get; }
    public GlobalSettings Settings { get; }
    public ViewModelActivator Activator { get; }


    public IEnumerable<SerialAdaptorSettings> Serial
    {
        get => Settings.Serial;
    }

    public IEnumerable<TCPAdaptorSettings> TCP
    {
        get => Settings.TCP;
    }

    public ICollection<TCPAdaptorSettings> Connections
    {
        get => TCPAdapter.Connections.Values;
    }

}