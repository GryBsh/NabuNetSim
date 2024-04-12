using Blazorise;
using DynamicData;
using Gry.Adapters;
using Gry.Settings;
using Nabu.Adaptors;
using Nabu.NetSim.UI.Models;
using Nabu.Network;
using Nabu.Settings;
using Nabu.Sources;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using YamlDotNet.Core.Tokens;

namespace Nabu.NetSim.UI.ViewModels;

public class AdaptorSettingsViewModel : ReactiveObject, IActivatableViewModel
{
    public AdaptorSettingsViewModel(
        GlobalSettings settings,
        HomeViewModel home,
        INabuNetwork network,
        //SettingsViewModel settingsModel,
        //StatusViewModel status,
        FilesViewModel files,
        SourceService sources,
        ProcessService process,
        SettingsProvider settingsProvider
    //IOptions<GlobalSettings> options
    )
    {
        //Settings = settings;
        Home = home;
        Network = network;
        Files = files;
        Sources = sources;
        Process = process;
        SettingsProvider = settingsProvider;
        Settings = settings;
        //Options = options.Value;
        //Status = status;
        SettingsModel = new SettingsModel<object>(string.Empty, new(), SettingsProvider);
        Activator = new();
        this.WhenActivated(
            disposables =>
            {

                /*Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    this.RaisePropertyChanged(nameof(BrowseDisabled));
                    this.RaisePropertyChanged(nameof(Selected));
                }).DisposeWith(disposables);*/

                Observable.Interval(TimeSpan.FromSeconds(10))
                .Subscribe(_ =>
                {

                    AllSerialPorts = SerialPort.GetPortNames();

                    this.RaisePropertyChanged(nameof(AllSerialPorts));
                    this.RaisePropertyChanged(nameof(AvailableSerialPorts));
                    this.RaisePropertyChanged(nameof(UserSerialPorts));

                }).DisposeWith(disposables);

                Home.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(Home.VisiblePage))
                    {
                        this.RaisePropertyChanged(nameof(SettingsPage));
                        if (Selected is not NullAdaptorSettings)
                        {

                            if (Home.IsVisible(VisiblePage.Settings) is Visibility.Visible)
                            {
                                SetSelected(new NullAdaptorSettings());

                            }
                        }
                        //SetSelected(new NullAdaptorSettings());
                        //this.RaisePropertyChanged(nameof(SettingsPage));
                    }
                };
            }
        );

        Task.Run(() =>
        {

            AllSerialPorts = SerialPort.GetPortNames();

            this.RaisePropertyChanged(nameof(AllSerialPorts));
            this.RaisePropertyChanged(nameof(AvailableSerialPorts));
            this.RaisePropertyChanged(nameof(UserSerialPorts));

        });


    }

    public ViewModelActivator Activator { get; }
    public bool AdaptorSelected => Selected is not NullAdaptorSettings;

    public bool BrowseDisabled
    {
        get
        {
            return (Selected is NullAdaptorSettings) ||
                   !(Selected is TCPAdaptorSettings tcp && tcp.Connection) && 
                   !(Selected is SerialAdaptorSettings serial && serial.Adapter?.State is AdapterState.Running);
        }
    }
    public static ICollection<TCPAdaptorSettings> Connections
    {
        get => TCPAdapter.Connections.Values;
    }

    //public StatusViewModel Status { get; }
    public FilesViewModel Files { get; }

    public bool HasMultipleImages
    {
        get
        {
            if (Selected is null or NullAdaptorSettings) return false;
            var programs = Network.Programs(Selected);
            var hasImages = programs.Count() > 1;
            var exploitEnabled = Network.Source(Selected)?.EnableExploitLoader is true;
            var isNotPakCycle = !programs.Any(p => p.Name == Constants.CycleMenuPak);
            return hasImages && (exploitEnabled || isNotPakCycle);
        }
    }

    //public SettingsViewModel Settings { get; }
    public HomeViewModel Home { get; }

    public bool IsClient => Selected is TCPAdaptorSettings t && t.Connection;

    public INabuNetwork Network { get; }

    public ProcessService Process { get; }
    public SettingsProvider SettingsProvider { get; }

    public AdaptorSettings Selected { get; set; } = new NullAdaptorSettings();

    public IEnumerable<string?> UserSerialPorts
        => from serial in Settings.Serial select serial.Port;

    public IEnumerable<string> AllSerialPorts { get; private set; } = [];

    public IEnumerable<string> AvailableSerialPorts
        => from port in AllSerialPorts
           from used in UserSerialPorts
           where port != used
           select port;


    public int NextFreePort => NabuLib.GetFreePort(5816);

    public GlobalSettings Settings { get; }

    public bool ShouldRun => Selected is TCPAdaptorSettings t && !t.Connection && Settings.EmulatorPath != string.Empty;

    public SourceService Sources { get; }

    public ObservableCollection<NabuProgram> Programs { get; internal set; } = [];


    public void SetFilesPath(string path)
    {
        Files.SetRootDirectory(Selected, path);
    }

    public SettingsModel? SettingsModel { get; set; } 

    public Visibility SettingsPage 
        =>  Selected is NullAdaptorSettings && 
            Home.IsVisible(VisiblePage.Settings) is Visibility.Visible ?
                Visibility.Visible :
                Visibility.Invisible;

    public static Dictionary<string, string> SectionIcons = new()
    {
        ["General"] = "fa-gear",
        ["Network"] = "fa-network",
        ["Port"] = "fa-plug",
        ["Source"] = "fa-database",
        ["Headless"] = "fa-box",
        ["Logs"] = "fa-file",
        ["Storage"] = "fa-save",

    };

    public IEnumerable<(string, string, SettingValue[])>? Sections =>
         SettingsModel?.Settings?.Where(s => ShowAdvanced || !s.Advanced)
                                 .GroupBy(s => s.Section)
                                 .OrderBy(s => s.Key)
                                 .Select(g => (g.Key, SectionIcons[g.Key], g.ToArray()));
    public bool ShowAdvanced { get; set; } = false;

    public void SetSelected<T>(SettingsModel<T> selected)
        where T : AdaptorSettings, new()
    {
        SettingsModel = selected;

        this.RaisePropertyChanged(nameof(SettingsModel));
        Select(selected.Current);

        this.RaisePropertyChanged(nameof(Sections));
    }

    void AdaptorSettingsChanged(SettingValue setting, AdaptorSettings? source, AdaptorSettings? current)
    {
        if (setting.Name == nameof(AdaptorSettings.Port) && source?.Enabled is true)
        {
            setting.ReadOnly = true;
            this.RaisePropertyChanged(nameof(SettingsModel));
        }
    }

    public void SetSelected(AdaptorSettings selected)
    {
        SettingsModel = selected switch
        {
            SerialAdaptorSettings => new SettingsModel<SerialAdaptorSettings>(
                selected.Port, 
                (SerialAdaptorSettings)selected, 
                SettingsProvider,
                AdaptorSettingsChanged
            ),
            TCPAdaptorSettings => new SettingsModel<TCPAdaptorSettings>(
                selected.Port, 
                (TCPAdaptorSettings)selected, 
                SettingsProvider,
                AdaptorSettingsChanged
            ),
            _ => null
        };

        this.RaisePropertyChanged(nameof(SettingsModel));
        Select(selected);
    }

    void RefreshPrograms()
    {
        Programs = new(SettingsModel?.Current switch
        {
            null => [],
            _ => Network.Programs(
                     SettingsModel?.Current as AdaptorSettings ??
                     new NullAdaptorSettings()
                 ).ToArray()
        });

        this.RaisePropertyChanged(nameof(Programs));
    }

    void Select(AdaptorSettings? selected)
    {
        Selected = selected ?? new NullAdaptorSettings();
        this.RaisePropertyChanged(nameof(Selected));
        
        if (Selected is NullAdaptorSettings)
        {
            if (Home.IsVisible(VisiblePage.Settings) is not Visibility.Visible) 
                Home.SetVisible(VisiblePage.Adaptors);
            Files.SetRootDirectory(Selected);
            Programs.Clear();
        }
        else
        {
            if (Home.IsVisible(VisiblePage.Settings) is not Visibility.Visible) 
                Home.SetVisible(VisiblePage.AdaptorSettings);
            if (string.IsNullOrWhiteSpace(Selected.Port))
            {
                if (Selected is TCPAdaptorSettings)
                    Selected.Port = NextFreePort.ToString();
                else
                    Selected.Port = AvailableSerialPorts.FirstOrDefault() ?? string.Empty;
            }
            if (selected is SerialAdaptorSettings serial && serial.BaudRate is 0)
            {
                serial.BaudRate = 115200;
            }

            if (SettingsModel != null)
                SettingsModel.PropertyChanged += (s, e) =>
                {
                    RefreshPrograms();                    Selected.SetChanged();
                };

            RefreshPrograms();
        }

    }

    public void SaveSettings()
    {
        ApplySettings();
        SettingsProvider.SaveSettings("Settings", Settings);
        Selected.ResetChanged();
    }

    public bool CanSave => Selected.ChangedSinceLoad;


    public void ApplySettings()
    {
        SettingsModel?.Apply();
        
    }

    public void RevertSettings()
    {
        SettingsModel?.Revert();
        //Selected.ResetChanged();
    }


}