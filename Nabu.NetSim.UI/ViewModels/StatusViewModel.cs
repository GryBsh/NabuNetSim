using Nabu.Adaptor;
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
        Settings settings
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
                    .Interval(TimeSpan.FromSeconds(5), RxApp.TaskpoolScheduler)
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
    public Settings Settings { get; }
    public ViewModelActivator Activator { get; }

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