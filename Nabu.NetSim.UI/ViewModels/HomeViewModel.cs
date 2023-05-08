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
using Nabu.Adaptor;
using Nabu.NetSim.UI.Models;
using Nabu.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using DynamicData.Binding;

namespace Nabu.NetSim.UI.ViewModels;


public class HomeViewModel : ReactiveObject
{
    public Settings Settings { get; }
    INabuNetwork Sources { get; }
    public List<LogEntry> Entries { get; private set; } = new List<LogEntry>();
    ISimulation? Simulation { get; }
    IRepository<LogEntry> Repository { get; }
    IMultiLevelCache Cache { get; }

    const string FeedUrl = "https://www.nabunetwork.com/feed/";
    public HomeViewModel(
        Settings settings,
        INabuNetwork sources,
        ISimulation simulation,
        IRepository<LogEntry> repository,
        IMultiLevelCache cache
    )
    {
        Cache = cache;
        Settings = settings;
        Sources = sources;
        Simulation = simulation;
        Repository = repository;
        Menu = new(this, Sources);


        //SourceNames = SourceFolders.Select(s => s.Name).ToArray();
        Task.Run(async () =>
        {
            RefreshLog();
            GetHeadlines();
            await Task.Delay(TimeSpan.FromSeconds(5));
            Loaded = true;
            this.RaisePropertyChanged(nameof(Loaded));
        });

        Observable.Interval(TimeSpan.FromSeconds(15))
                 .Subscribe(_ => {
                     this.RaisePropertyChanged(nameof(Connections));
                     //GC.Collect();
                 });

        Observable.Interval(TimeSpan.FromMinutes(15))
                 .Subscribe(_ => {
                     RefreshLog();
                     //GC.Collect();
                 });

        Observable.Interval(TimeSpan.FromMinutes(10))
                  .Subscribe(_ => {
                      GetHeadlines();
                      //GC.Collect();
                  });


    }

    public bool Loaded { get; set; } = false;

    void RefreshLog()
    {
        var now = DateTime.Now;
        //var add = .Select(e => e.Timestamp > LastUpdate).OrderByDescending(e => e.Timestamp);
        //Entries.AddRange(add);
        //var count = Entries.Count;

        Entries = Repository.SelectAll()
                            .OrderByDescending(e => e.Timestamp)
                            .ToList();

        if (LogVisible is false) return;
        this.RaisePropertyChanged(nameof(Entries));
        this.RaisePropertyChanged(nameof(LogPages));
        //this.RaisePropertyChanged(nameof(CurrentLogPage));
        //GC.Collect();

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
    public string LogButtonText { get => LogVisible ? "Hide Log" : "Show Log"; }

    public bool LogVisible { get; set; } = false;
    public Visibility LogVisibility => LogVisible ? Visibility.Visible : Visibility.Invisible;
    public string LogDateTimeFormat { get; } = "yyyy-MM-dd HH:mm:ss.ffff";
    public int LogPage { get; set; } = 1;
    public int LogPageSize { get; set; } = 100;

    string logSearch = string.Empty;

    public string LogSearch
    {
        get {
            return logSearch;
        }
        set
        {
            logSearch = value ?? string.Empty;
            
            this.RaisePropertyChanged(nameof(LogSearch));
            this.RaisePropertyChanged(nameof(LogPages));
            this.RaisePropertyChanged(nameof(CurrentLogPage));
        }
    }

    public int LogPages {
        get {
            var total = Entries.Count;
            if (total is 0) return 0;
            var count = (int)Math.Ceiling((double)(total / LogPageSize));
            return count < 1 ? 1 : count;
        }
    }

    public ICollection<LogEntry> CurrentLogPage {
        get
        {
            return Entries
                    .Skip((LogPage - 1) * LogPageSize)
                    .Take(LogPageSize)
                    .Select(
                        e =>
                        {
                            var term = LogSearch.ToLowerInvariant();
                            if (
                                LogSearch != string.Empty && (
                                    e.Timestamp.ToString(LogDateTimeFormat).ToLowerInvariant().Contains(term) ||
                                    e.Name.ToLowerInvariant().Contains(term) ||
                                    e.Message.ToLowerInvariant().Contains(term)
                                )
                            ) e.Highlight = true;
                            else e.Highlight = false;

                            return e;
                        }
                    ).ToList();                     
        }
    }

    public void SetLogPage(string page) => LogPage = int.Parse(page);
    public void LogPageBack()
    {
        if (LogPage is 1) return;
        LogPage -= 1;
        this.RaisePropertyChanged(nameof(LogPage));
        this.RaisePropertyChanged(nameof(CurrentLogPage));
    }

    public void LogPageForward()
    {
        if (LogPage == LogPages || LogPages is 0) return;
        LogPage += 1;
        this.RaisePropertyChanged(nameof(LogPage));
        this.RaisePropertyChanged(nameof(CurrentLogPage));
    }

    public bool IsActiveLogPage(int page) 
        => LogPage == page || page is 0;

    public void LogSearchClear()
    {
        LogSearch = string.Empty;
        this.RaisePropertyChanged(nameof(LogSearch));
    }

    public void ToggleLog()
    {
        LogVisible = !LogVisible;
        this.RaisePropertyChanged(nameof(LogSearch));
        this.RaisePropertyChanged(nameof(LogPages));
        this.RaisePropertyChanged(nameof(Entries));
        this.RaisePropertyChanged(nameof(CurrentLogPage));
        this.RaisePropertyChanged(nameof(LogVisible));
    }

    public ICollection<TickerItem> Headlines { get; set; } = Array.Empty<TickerItem>();

    public async void GetHeadlines()
    {
        var headlines = await Cache.GetOrSetAsync(
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
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            new() { }
        ) ?? Array.Empty<TickerItem>();

        Headlines = headlines.ToList();

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

    public IconName AdaptorButtonIcon(AdaptorSettings settings)
    {
        return settings.State switch
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
        get => Serial.Any(s => s.State is ServiceShould.Run) ||
               TCP.Any(t => t.State is ServiceShould.Run);
    }

    public bool HasMultipleImages(AdaptorSettings? settings)
    {
        if (settings is null or NullAdaptorSettings) return false;
        var programs = Sources.Programs(settings);
        var hasMultipleImages = programs.Count() > 1;
        var exploitEnabled = Sources.Source(settings)?.EnableExploitLoader is true;
        var isNotPakCycle = !programs.Any(p => p.Name == Constants.CycleMenuPak);
        return hasMultipleImages && (exploitEnabled || isNotPakCycle);
    }

    public ICollection<ProgramSource> SourceFolders
    {
        get => Settings.Sources;
        set => Settings.Sources = value.ToList();
    }

    public string[] SourceNames => SourceFolders.Select(f => f.Name).ToArray();
      
    

    public bool SelectorVisible { get; set; } = false;
    public AdaptorSettings[] SelectorAdaptor { get; set; } = Array.Empty<AdaptorSettings>();
    

}

