using Blazorise;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Hosting;
using Nabu.NetSim.UI;
using Nabu.Network;
using ReactiveUI;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reactive;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels;

public class HomeViewModel : ReactiveObject
{

    AppLog AppLog { get; }
    Settings Settings { get; }
    NabuNetService Images { get; }
    public IEnumerable<string> Entries { get; set; }

    ISimulation? Simulation { get; }

    public HomeViewModel(AppLog appLog, Settings settings, NabuNetService images, IHostedService simulation)
    {
        AppLog = appLog;
        Settings = settings;
        Images = images;
        Simulation = simulation as ISimulation;

        var sort = SortExpressionComparer<LogEntry>.Descending(e => e.Timestamp);
        var throttle = TimeSpan.FromSeconds(1);

        SourceNames = SourceFolders.Select(s => s.Name).ToArray();

        AppLog.Entries
            .Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Sort(sort)
            .Bind(out var log)
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .SubscribeOn(RxApp.TaskpoolScheduler)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(Entries)));

        Entries = log.Where(e => e.Timestamp > DateTime.Now.AddMinutes(-AppLog.Interval))
                     .Select(e => $"{e.Timestamp:yyyy-MM-dd | HH:mm:ss.ff} | {e.Message}");
    }

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
            ServiceShould.Continue => "btn btn-danger btn-sm d-flex",
            ServiceShould.Restart => "btn btn-warning btn-sm d-flex",
            ServiceShould.Stop => "btn btn-success btn-sm d-flex",
            _ => "btn btn-secondary btn-sm d-flex"
        };
    }

    public string AdaptorStatus(AdaptorSettings settings)
    {
        return settings.Next switch
        {
            ServiceShould.Continue => "Running",
            ServiceShould.Restart => "Stopping",
            ServiceShould.Stop => "Stopped",
            _ => "Unknown"
        };
    }

    public IconName AdaptorButtonIcon(AdaptorSettings settings)
    {
        return settings.Next switch
        {
            ServiceShould.Continue => IconName.Stop,
            ServiceShould.Restart => IconName.Stop,
            ServiceShould.Stop => IconName.Play,
            _ => IconName.Play
        };
    }

    public void ToggleAdaptor(AdaptorSettings settings)
    {
        settings.Next = settings.Next is ServiceShould.Continue ?
                        ServiceShould.Stop :
                        ServiceShould.Continue;
    }

    public bool AllAdaptorsCanStop
    {
        get => Serial.Any(s => s.Next is ServiceShould.Continue) ||
               TCP.Any(t => t.Next is ServiceShould.Continue);
    }

    public string AllAdaptorButtonClass(AdaptorSettings settings)
    {
        return settings.Next switch
        {
            ServiceShould.Continue => "btn btn-danger btn-sm d-flex",
            ServiceShould.Restart => "btn btn-warning btn-sm d-flex",
            ServiceShould.Stop => "btn btn-success btn-sm d-flex",
            _ => "btn btn-secondary btn-sm d-flex"
        };
    }

    public void ToggleAllAdaptors()
    {
        foreach (var s in Serial)
        {
            s.Next = s.Next is ServiceShould.Continue ?
                     ServiceShould.Stop :
                     ServiceShould.Continue;
        }

        foreach (var t in TCP)
        {
            t.Next = t.Next is ServiceShould.Continue ?
                     ServiceShould.Stop :
                     ServiceShould.Continue;
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
    public void ToggleSettings()
    {
        SettingsVisible = !SettingsVisible;
    }

    public string LogButtonClass { get => LogVisible ? "btn btn-danger btn-sm" : "btn btn-success btn-sm"; }
    public string LogButtonText { get => LogVisible ? "Hide Log" : "Show Log"; }

    public bool LogVisible { get; set; } = false;
    public void ToggleLog()
    {
        LogVisible = !LogVisible;
    }

    public bool SelectorVisible { get; set; } = false;
    public AdaptorSettings[] SelectorAdaptor { get; set; } = Array.Empty<AdaptorSettings>();
    public void ToggleSelector(AdaptorSettings? settings)
    {

        if (SelectorVisible is false && settings is not null)
        {
            Task.Run(async () =>
            {
                await Images.RefreshSources();
                SelectorAdaptor = new[] { settings };
                AvailableImages = Images.Programs(settings).Select(p => (p.DisplayName, p.Name));
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

