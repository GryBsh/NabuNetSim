using ReactiveUI;
using System.Reactive.Linq;
using Nabu.Network;
using LiteDB;
using Nabu.Models;
using Nabu.NetSim.UI.Services;
using Nabu.NetSim.UI.Models;
using Blazorise;
using System.Reactive.Disposables;

namespace Nabu.NetSim.UI.ViewModels;



public class HomeViewModel : ReactiveObject, IActivatableViewModel
{
    public Settings Settings { get; }
    public INabuNetwork Sources { get; }
    public ISimulation Simulation { get; }
    HeadlineService News { get; }
    public bool Visible { get; set; } = true;

    public ViewModelActivator Activator { get; }


    public HomeViewModel(
        Settings settings,
        INabuNetwork sources,
        ISimulation simulation,
        HeadlineService news
    )
    {
        Settings = settings;
        Sources = sources;
        Simulation = simulation;
        News = news;
        Activator = new();

        this.WhenActivated(
            disposables =>
            {
                Observable.Interval(TimeSpan.FromMinutes(10), RxApp.TaskpoolScheduler)
                    .Subscribe(_ => GetHeadlines())
                    .DisposeWith(disposables);
            }
        );

        Task.Run(async () =>
        {
            //Log!.RefreshLog();
            GetHeadlines();
            await Task.Delay(TimeSpan.FromSeconds(5));
            Loaded = true;
        });

        
    }

    bool loaded = false;
    public bool Loaded
    {
        get => loaded;
        set
        {
            loaded = value;
            this.RaisePropertyChanged();
        }
    }

    static string[] Phrases = new[] {
        "👁️🚢👿",
        "Assimilation in progress",
        "Admiral! There be whales here!",
        "Ay Sir, I'm working on it!",
        "You should visit NABUNetwork.com",
        "Hey Mr. 🦉",
        "Standby for NABUfall",
        "Your honor, I object to this preposterous poppycock",
        "It works for us now, Comrade",
        "Buy Pants",
        "2 NABUs and a KayPro walk into a bar...",
        "💣 0.015625 MEGA POWER 💣",
        "9/10 Doctors would prefer not to endorse this product",
        "NABU4Ever!",
        "👸Beware the wrath of King NABU 👸",
        "☎️ Please stay on the line, your call is important to us ☎️",
        "🎵 Never gonna give you up. Never gonna let you down 🎵",
        "Excuse me human, can I interest you in this pamphlet on the kingdom of NABU?"
    };

    public string Phrase => Phrases[Random.Shared.Next(0, Phrases.Length)];

    public ICollection<TickerItem> Headlines => News.Headlines.ToList();

    public void GetHeadlines()
    {
        this.RaisePropertyChanged(nameof(Headlines));

    }

    public void ToggleAdaptor(AdaptorSettings settings)
    {
        if (settings is TCPAdaptorSettings connection && connection.Connection)
            connection.ListenTask?.Cancel();
        else 
            Simulation?.ToggleAdaptor(settings);
    }

    public VisiblePage VisiblePage { get; set; } = VisiblePage.Adaptors;
    VisiblePage LastPage { get; set; } = VisiblePage.Adaptors;

    

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

