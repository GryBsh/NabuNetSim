using Blazorise;
using Nabu.Services;
using ReactiveUI;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels;

public class AdaptorViewModel : ReactiveObject, IActivatableViewModel
{
    public AdaptorViewModel(HomeViewModel home, AdaptorSettingsViewModel menu, Settings settings, ProcessService process, ISimulation simulation)
    {
        Home = home;
        Menu = menu;
        Settings = settings;
        Process = process;
        Simulation = simulation;
        this.WhenActivated(disposables =>
        {
            Observable.Interval(TimeSpan.FromSeconds(10))
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
    public Settings Settings { get; }
    public ISimulation Simulation { get; }

    public string AdaptorButtonText(AdaptorSettings settings)
    {
        return settings.State switch
        {
            ServiceShould.Run => "Stop Adaptor",
            _ => "Start Adaptor"
        };
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

    public void ToggleAdaptor(AdaptorSettings settings)
    {
        if (settings is TCPAdaptorSettings connection && connection.Connection)
            connection.ListenTask?.Cancel();
        else
            Simulation?.ToggleAdaptor(settings);
    }
}