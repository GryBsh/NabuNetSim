using Blazorise;
using CodeHollow.FeedReader;
using DynamicData;
using DynamicData.Binding;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Nabu.NetSim.UI;
using ReactiveUI;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Xml;
using static System.Net.WebRequestMethods;
using Nabu.Network;

namespace Nabu.NetSim.UI.ViewModels;



public class HomeViewModel : ReactiveObject
{
    public Settings Settings { get; }
    NabuNetwork Sources { get; }
    public IEnumerable<string> Entries { get; set; }

    ISimulation? Simulation { get; }
    

    public HomeViewModel(
        Settings settings, 
        NabuNetwork sources, 
        ISimulation simulation
    )
    {
        Settings = settings;
        Sources = sources;
        Simulation = simulation;
        Menu = new(this, Sources);

        var sort = SortExpressionComparer<LogEntry>.Descending(e => e.Timestamp);
        var throttle = TimeSpan.FromSeconds(1);

        SourceNames = SourceFolders.Select(s => s.Name).ToArray();

        AppLog.LogEntries
            .Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Sort(sort)
            .Bind(out var log)
            .Throttle(TimeSpan.FromSeconds(5))
            .SubscribeOn(RxApp.TaskpoolScheduler)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(Entries)));

        Entries = log.Where(e => e.Timestamp > DateTime.Now.AddMinutes(-AppLog.Interval))
                     .Select(e => $"{e.Timestamp:yyyy-MM-dd | HH:mm:ss.ff} | {e.Message}");
        
        Task.Run(async () => Headlines = await GetHeadlines());
        Observable.Interval(TimeSpan.FromMinutes(30), ThreadPoolScheduler.Instance)
                  .Subscribe(async _ => {
                      Headlines = await GetHeadlines();
                      
                  });
    }

    public IEnumerable<(string, string)> Headlines { get; set; } = Array.Empty<(string, string)>();

    public async Task<IEnumerable<(string,string)>> GetHeadlines()
    {
        var url = "https://www.nabunetwork.com/feed/";
        try
        {
            var feed = await FeedReader.ReadAsync(url);
            return feed.Items.Select(item => (item.Title, item.Link)).ToArray();
        }
        catch
        {
            return Array.Empty<(string, string)>();
        }
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
        return programs.Count() > 1 && programs.Count(p => p.Name == "000001") == 0;
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

