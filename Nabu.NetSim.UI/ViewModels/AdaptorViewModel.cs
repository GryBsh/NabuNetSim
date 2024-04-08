using Gry.Adapters;
using Nabu.Network;
using Nabu.Settings;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels;

public class AdaptorViewModel : ReactiveObject, IActivatableViewModel
{
    public AdaptorViewModel(HomeViewModel home, AdaptorSettingsViewModel menu, GlobalSettings settings, ProcessService process, ISimulation simulation)
    {
        Home = home;
        Menu = menu;
        Settings = settings;
        Process = process;
        Simulation = simulation;
        this.WhenActivated(disposables =>
        {
            Observable.Interval(TimeSpan.FromSeconds(1))
               .Subscribe(_ =>
               {
                   this.RaisePropertyChanged("AdaptorStatus");
               }).DisposeWith(disposables);
        });
    }

    public ViewModelActivator Activator { get; } = new();
    public HomeViewModel Home { get; }
    public AdaptorSettingsViewModel Menu { get; }
    public ProcessService Process { get; }
    public GlobalSettings Settings { get; }
    public ISimulation Simulation { get; }

    public string AdaptorButtonText(AdaptorSettings settings)
    {
        return settings.Adapter?.State switch
        {
            AdapterState.Running => "Stop Adaptor",
            _ => "Start Adaptor"
        };
    }

    public string AdaptorStatus(AdaptorSettings settings)
    {
        return settings.Adapter?.State switch
        {
            AdapterState.Starting => "Starting",
            AdapterState.Running => "Running",
            AdapterState.Stopping => "Stopping",
            AdapterState.Stopped => "Stopped",
            _ => "Unknown"
        };
    }

    public void ToggleAdaptor(AdaptorSettings settings)
    {
        if (settings is TCPAdaptorSettings connection && connection.Connection)
            connection.ListenTask?.Cancel();
        else
            Simulation?.ToggleAdaptor(settings);
    }
}