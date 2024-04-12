using Blazorise;
using Nabu.NetSim.UI.Models;
using Nabu.Network;
using Nabu.Services;
using Nabu.Settings;
using ReactiveUI;

namespace Nabu.NetSim.UI.ViewModels;

public class HomeViewModel : ReactiveObject, IActivatableViewModel
{
    

    private bool loaded = false;

    public HomeViewModel(
        GlobalSettings settings,
        INabuNetwork sources,
        ISimulation simulation
    )
    {
        Settings = settings;
        Sources = sources;
        Simulation = simulation;

        Activator = new();

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            Loaded = true;
        });
    }

    public ViewModelActivator Activator { get; }

    public bool Loaded
    {
        get => loaded;
        set
        {
            loaded = value;
            this.RaisePropertyChanged();
        }
    }

    public string Phrase => NabuNetwork.Phrase();
    public GlobalSettings Settings { get; }
    public ISimulation Simulation { get; }
    public INabuNetwork Sources { get; }
    public bool Visible { get; set; } = true;
    public VisiblePage VisiblePage { get; set; } = VisiblePage.Adaptors;

    private VisiblePage LastPage { get; set; } = VisiblePage.Adaptors;

    public Visibility IsVisible(VisiblePage page) => VisiblePage == page ? Visibility.Visible : Visibility.Invisible;

    public void SetVisible(VisiblePage visible, bool setLastPage = true)
    {
        if (setLastPage) LastPage = VisiblePage;
        VisiblePage = visible;
        this.RaisePropertyChanged(nameof(VisiblePage));
    }

    

    public void ToggleVisible(VisiblePage page)
    {
        if (VisiblePage == page)
            SetVisible(LastPage, false);
        else
            SetVisible(page);
    }
}