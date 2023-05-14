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
using Splat;
using DynamicData.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Nabu.NetSim.UI.ViewModels;


public class HomeViewModel : ReactiveObject
{
    public Settings Settings { get; }
    public INabuNetwork Sources { get; }
    public ISimulation Simulation { get; }
    //IRepository<LogEntry> Repository { get; }
    IMultiLevelCache Cache { get; }

    //public MenuViewModel? Menu { get; set; }
    //public LogViewModel? Log { get; set; }
    //public StatusViewModel? Status { get; set; }

    const string FeedUrl = "https://www.nabunetwork.com/feed/";
    public HomeViewModel(
        Settings settings,
        INabuNetwork sources,
        ISimulation simulation,
        //MenuViewModel menu,
        //LogViewModel log,
        //StatusViewModel status,
        IMultiLevelCache cache
    )
    {
        Cache = cache;
        Settings = settings;
        Sources = sources;
        Simulation = simulation;
        //Repository = repository;
        //Menu = menu;
        //Log = log;
        //Status = status;


        //SourceNames = SourceFolders.Select(s => s.Name).ToArray();
        Task.Run(async () =>
        {
            //Log!.RefreshLog();
            GetHeadlines();
            await Task.Delay(TimeSpan.FromSeconds(5));
            Loaded = true;
            
            //await Task.Delay(TimeSpan.FromSeconds(5));
            
        });



        Observable.Interval(TimeSpan.FromMinutes(10))
                  .Subscribe(_ => {
                      GetHeadlines();
                      //GC.Collect();
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
    /*
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

    }*/

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

    public ICollection<TickerItem> Headlines { get; set; } = new List<TickerItem>();

    public async void GetHeadlines()
    {
        
        async Task<IEnumerable<TickerItem>> Get()
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
        }
        var headlines = await Cache.GetOrSetAsync(
            "headlines",
            c => Get(),
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }
        );
        Headlines = headlines is not null && headlines.Any() ? headlines.ToList() : Array.Empty<TickerItem>().ToList();
            
        
        this.RaisePropertyChanged(nameof(Headlines));

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
        Simulation?.ToggleAdaptor(settings);
    }

}

