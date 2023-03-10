using Blazorise;
using CodeHollow.FeedReader;
using ReactiveUI;
using System.IO.Ports;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nabu.Network;
using LiteDB;
using LiteDb.Extensions.Caching;
using DynamicData;

namespace Nabu.NetSim.UI.ViewModels;

public record TickerItem(string Title, string Link);

public class HomeViewModel : ReactiveObject
{
    public Settings Settings { get; }
    INabuNetwork Sources { get; }
    public List<LogEntry> Entries { get; private set; } = new List<LogEntry>();
    ISimulation? Simulation { get; }
    IRepository Repository { get; }
    IMultiLevelCache Cache { get; }

    const string FeedUrl = "https://www.nabunetwork.com/feed/";

    public HomeViewModel(
        Settings settings, 
        INabuNetwork sources, 
        ISimulation simulation,
        IRepository repository,
        IMultiLevelCache cache
    )
    {
        Cache = cache;
        Settings = settings;
        Sources = sources;
        Simulation = simulation;
        Repository = repository;
        Menu = new(this, Sources);

        SourceNames = SourceFolders.Select(s => s.Name).ToArray();

        PrimeLog();
        GetHeadlines();

        Observable.Interval(TimeSpan.FromMinutes(1))
            .Subscribe(_ => {
                RefreshLog();
                GC.Collect();
            });
        
        Observable.Interval(TimeSpan.FromMinutes(10))
                  .Subscribe(_ => {
                      GetHeadlines();
                      GC.Collect();
                  });
    }
    DateTime LastUpdate = DateTime.Now;
    async void PrimeLog()
    {
        await Task.Run(() =>
        {
            var now = DateTime.Now;
            var cutoff = now.AddMinutes(-Settings.MaxUIEntryAgeMinutes);

            Entries =
                Repository.Collection<LogEntry>()
                .Find(e => e.Timestamp > cutoff)
                .OrderByDescending(e => e.Timestamp)
                .ToList();
            this.RaisePropertyChanged(nameof(Entries));
            LastUpdate = now;
        });
    }

    void RefreshLog()
    {
        var now = DateTime.Now;
        var cutoff = now.AddMinutes(-Settings.MaxUIEntryAgeMinutes);
        var add = Repository.Collection<LogEntry>().Find(e => e.Timestamp > LastUpdate).OrderByDescending(e => e.Timestamp);
        var remove = Entries.Where(e => e.Timestamp < cutoff);
        Entries.AddRange(add);
        Entries.RemoveMany(remove);
        Entries = Entries.OrderByDescending(e => e.Timestamp).ToList();
        LastUpdate = now;
        this.RaisePropertyChanged(nameof(Entries));
    }

    public ICollection<TickerItem> Headlines { get; set; } = Array.Empty<TickerItem>();

  
    public async void GetHeadlines()
    {
        Headlines = (
            (await Cache.GetOrSetAsync(
                FeedUrl,
                async cancel =>
                {
                    try
                    {
                        var feed = await FeedReader.ReadAsync(FeedUrl);
                        var items = feed.Items.Take(4).Select(i => new TickerItem(i.Title, i.Link));
                        return items;
                    }
                    catch
                    {
                        return Array.Empty<TickerItem>();
                    }
                },
                new() { AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromMinutes(5) },
                new() { AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromMinutes(10) }
            )) ?? Array.Empty<TickerItem>()
        ).ToList();

        this.RaisePropertyChanged(nameof(Headlines));

    }

    public MenuViewModel Menu { get; set; } 

    public ICollection<SerialAdaptorSettings> Serial
    {
        get => Settings.Adaptors.Serial;
    }

    public ICollection<TCPAdaptorSettings> TCP
    {
        get => Settings.Adaptors.TCP;
    }


    public string AdaptorButtonClass(AdaptorSettings settings)
    {
        return settings.Next switch
        {
            ServiceShould.Run => "btn btn-danger btn-sm d-flex",
            ServiceShould.Restart => "btn btn-warning btn-sm d-flex",
            ServiceShould.Stop => "btn btn-success btn-sm d-flex",
            _ => "btn btn-secondary btn-sm d-flex"
        };
    }

    public string AdaptorStatus(AdaptorSettings settings)
    {
        return settings.Next switch
        {
            ServiceShould.Run => "Running",
            ServiceShould.Restart => "Stopping",
            ServiceShould.Stop => "Stopped",
            _ => "Unknown"
        };
    }

    public IconName AdaptorButtonIcon(AdaptorSettings settings)
    {
        return settings.Next switch
        {
            ServiceShould.Run => IconName.Stop,
            ServiceShould.Restart => IconName.Stop,
            ServiceShould.Stop => IconName.Play,
            _ => IconName.Play
        };
    }

    public void ToggleAdaptor(AdaptorSettings settings)
    {
        Simulation?.ToggleAdaptor(settings);
    }

    public bool AllAdaptorsCanStop
    {
        get => Serial.Any(s => s.Next is ServiceShould.Run) ||
               TCP.Any(t => t.Next is ServiceShould.Run);
    }

    public bool HasMultipleImages(AdaptorSettings? settings)
    {
        if (settings is null or NullAdaptorSettings) return false;
        var programs = Sources.Programs(settings);
        return programs.Count() > 1 && programs.Count(p => p.Name == Constants.CycleMenuPak) == 0;
    }

    public void ToggleAllAdaptors()
    {
        foreach (var s in Serial)
        {
            s.Next = s.Next is ServiceShould.Run ?
                     ServiceShould.Stop :
                     ServiceShould.Run;
        }

        foreach (var t in TCP)
        {
            t.Next = t.Next is ServiceShould.Run ?
                     ServiceShould.Stop :
                     ServiceShould.Run;
        }
    }

    public ICollection<ProgramSource> SourceFolders
    {
        get => Settings.Sources;
        set => Settings.Sources = value.ToList();
    }

    public string[] SourceNames { get; set; } = Array.Empty<string>();
    public string[] SerialPortNames { get; set; } = SerialPort.GetPortNames();
    public IEnumerable<(string, string)> AvailableImages { get; private set; } = Array.Empty<(string, string)>();

    

    public bool SettingsVisible { get; set; } = false;
  
    public string LogButtonText { get => LogVisible ? "Hide Log" : "Show Log"; }

    

    public bool LogVisible { get; set; } = false;
    public void ToggleLog()
    {
        LogVisible = !LogVisible;
        this.RaisePropertyChanged(nameof(LogVisible));
    }

    public bool SelectorVisible { get; set; } = false;
    public AdaptorSettings[] SelectorAdaptor { get; set; } = Array.Empty<AdaptorSettings>();
    public void ToggleSelector(AdaptorSettings? settings)
    {

        if (SelectorVisible is false && settings is not null)
        {
            Task.Run(() =>
            {
                //await Sources.RefreshSources();
                SelectorAdaptor = new[] { settings };
                AvailableImages = Sources.Programs(settings).Select(p => (p.DisplayName, p.Name));
                SelectorVisible = true;
                this.RaisePropertyChanged(nameof(SelectorVisible));
                this.RaisePropertyChanged(nameof(AvailableImages));
                this.RaisePropertyChanged(nameof(SelectorVisible));
            });

            return;
        }
        else if (SelectorVisible is true)
        {
            SelectorVisible = false;
            SelectorAdaptor = Array.Empty<AdaptorSettings>();
            AvailableImages = Array.Empty<(string, string)>();
        }
    }

}

