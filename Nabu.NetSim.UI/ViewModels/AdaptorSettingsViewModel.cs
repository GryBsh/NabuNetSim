using Blazorise;
using DynamicData;
using Nabu.Adaptor;
using Nabu.NetSim.UI.Models;
using Nabu.Network;
using Nabu.Services;
using ReactiveUI;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels;

public class AdaptorSettingsViewModel : ReactiveObject, IActivatableViewModel
{
    public AdaptorSettingsViewModel(
        Settings settings,
        HomeViewModel home,
        INabuNetwork network,
        //SettingsViewModel settingsModel,
        //StatusViewModel status,
        FilesViewModel files,
        SourceService sources,
        ProcessService process
    )
    {
        //Settings = settings;
        Home = home;
        Network = network;
        Files = files;
        Sources = sources;
        Process = process;
        Settings = settings;
        //Status = status;
        Activator = new();
        this.WhenActivated(
            disposables =>
            {
                Observable.FromEventPattern<ProgramSource>(
                    add => Sources.SourceChanged += add,
                    remove => Sources.SourceChanged -= remove
                )
                .Subscribe(_ => UpdateImages())
                .DisposeWith(disposables);

                Observable.Interval(TimeSpan.FromSeconds(10))
                .Subscribe(_ =>
                {
                    this.RaisePropertyChanged(nameof(RunAvailable));
                    this.RaisePropertyChanged(nameof(BrowseVisible));
                }).DisposeWith(disposables);
            }
        );

        Task.Run(UpdateImages);
    }

    public ViewModelActivator Activator { get; }
    public bool AdaptorSelected => Selected is not NullAdaptorSettings;

    public Visibility BrowseVisible
        => (Selected is TCPAdaptorSettings tcp && tcp.Connection) ||
            (Selected is SerialAdaptorSettings serial && serial.Running) ?
                Visibility.Visible :
                Visibility.Invisible;

    public ICollection<TCPAdaptorSettings> Connections
    {
        get => TCPAdaptor.Connections.Values.ToList();
    }

    //public StatusViewModel Status { get; }
    public FilesViewModel Files { get; }

    public bool HasMultipleImages
    {
        get
        {
            if (Selected is null or NullAdaptorSettings) return false;
            var programs = Network.Programs(Selected);
            var hasMultipleImages = programs.Count() > 1;
            var exploitEnabled = Network.Source(Selected)?.EnableExploitLoader is true;
            var isNotPakCycle = !programs.Any(p => p.Name == Constants.CycleMenuPak);
            return hasMultipleImages && (exploitEnabled || isNotPakCycle);
        }
    }

    //public SettingsViewModel Settings { get; }
    public HomeViewModel Home { get; }

    public ObservableCollection<AvailableImage> Images { get; } = new ObservableCollection<AvailableImage>();
    public bool IsClient => Selected is TCPAdaptorSettings t && t.Connection;
    public INabuNetwork Network { get; }

    public ProcessService Process { get; }

    public bool RunAvailable
        =>  ShouldRun &&
            Selected.Running &&
            (EmulatorProcess is null || EmulatorProcess.Value.IsCancellationRequested);

    public Visibility RunVisibility => ShouldRun ? Visibility.Visible : Visibility.Invisible;
    public AdaptorSettings Selected { get; set; } = new NullAdaptorSettings();

    public ICollection<SerialAdaptorSettings> Serial
    {
        get => Settings.Adaptors.Serial;
    }

    public Settings Settings { get; }
    public bool ShouldRun => (Selected is TCPAdaptorSettings t && !t.Connection && Settings.EmulatorPath != string.Empty);
    public string[] SourceNames => Sources.All().Select(f => f.Name).ToArray();
    public SourceService Sources { get; }

    public ICollection<TCPAdaptorSettings> TCP
    {
        get => Settings.Adaptors.TCP;
    }

    private CancellationToken? EmulatorProcess { get; set; }

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

    public string AdaptorButtonText(AdaptorSettings settings)
    {
        return settings.State switch
        {
            ServiceShould.Run => "Stop Adaptor",
            _ => "Start Adaptor"
        };
    }

    public void RunEmulator()
    {
        if (!RunAvailable)
            return;

        EmulatorProcess = Process.Start(Settings.EmulatorPath);
    }

    public void SetFilesPath(string path)
    {
        Files.SetRootDirectory(Selected, path);
    }

    public void SetImage(string image)
    {
        Selected.Program = image switch
        {
            null => string.Empty,
            Constants.CycleMenuPak => string.Empty,
            _ => image
        };
        this.RaisePropertyChanged(nameof(Selected));
        //NotifyChange();
    }

    public void SetSelected(AdaptorSettings selected)
    {
        Selected = selected;
        if (Selected is NullAdaptorSettings)
        {
            Home.SetVisible(VisiblePage.Adaptors);
            Files.SetRootDirectory(Selected);
        }
        else
        {
            Home.SetVisible(VisiblePage.AdaptorSettings);
        }

        UpdateImages();
        //SetVisible(MenuPage.AdaptorSettings);

        this.RaisePropertyChanged(nameof(AdaptorSelected));
        this.RaisePropertyChanged(nameof(Selected));
        this.RaisePropertyChanged(nameof(Images));
        this.RaisePropertyChanged(nameof(HasMultipleImages));
        this.RaisePropertyChanged(nameof(IsClient));
        //NotifyChange();
    }

    public void SetSource(string value)
    {
        Selected.Source = value;
        UpdateImages();
        var first = Images.FirstOrDefault()?.Name;

        Selected.Program = first switch
        {
            null => string.Empty,
            Constants.CycleMenuPak => string.Empty,
            _ => first
        };

        this.RaisePropertyChanged(nameof(Selected));
        //NotifyChange();
    }

    public void UpdateImages()
    {
        var images = Selected switch
        {
            NullAdaptorSettings => Array.Empty<AvailableImage>(),
            _ => Network.Programs(Selected).Where(p => p.DisplayName != string.Empty).Select(p => new AvailableImage(p.DisplayName, p.Name))
        };
        Images.Clear();
        Images.AddRange(images);
        this.RaisePropertyChanged(nameof(Images));
        this.RaisePropertyChanged(nameof(HasMultipleImages));
        this.RaisePropertyChanged(nameof(SourceNames));
        this.RaisePropertyChanged(nameof(Connections));
    }

    private void NotifyChange()
    {
        //Home.RaisePropertyChanged(nameof(Home.Menu));
    }
}