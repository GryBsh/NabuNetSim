using Blazorise;
using DynamicData;
using Nabu.NetSim.UI.Models;
using Nabu.Network;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Nabu.Adaptor;
using Nabu.Services;
using System.Reactive.Disposables;

namespace Nabu.NetSim.UI.ViewModels;

public record AvailableImage(string DisplayName, string Name);

public class AdaptorSettingsViewModel : ReactiveObject, IActivatableViewModel
{
    public AdaptorSettingsViewModel(
        Settings settings,
        HomeViewModel home, 
        INabuNetwork network, 
        //SettingsViewModel settingsModel,
        //StatusViewModel status,
        FilesViewModel files,
        SourceService sources
    )
    {
        Settings = settings;
        Home = home;
        Network = network;
        Files = files;
        Sources = sources;
        //Settings = settingsModel;
        //Status = status;
        Activator = new();
        this.WhenActivated(
            disposables =>
            {
                Observable
                    .Interval(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                    .Subscribe(_ => UpdateImages())
                    .DisposeWith(disposables);
            }
        );
        

        Task.Run(UpdateImages);
    }
    public Settings Settings { get; }
    //public SettingsViewModel Settings { get; }
    public HomeViewModel Home { get;  }
    public INabuNetwork Network { get; }
    public SourceService Sources { get; }
    //public StatusViewModel Status { get; } 
    public FilesViewModel Files { get; }
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

    void NotifyChange()
    {
        //Home.RaisePropertyChanged(nameof(Home.Menu));
    }
    
    public AdaptorSettings Selected { get; set; } = new NullAdaptorSettings();
    public bool AdaptorSelected => Selected is not NullAdaptorSettings;
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
        this.RaisePropertyChanged(nameof(IsClient));
        //NotifyChange();
    }

    public bool IsClient => Selected is TCPAdaptorSettings t && t.Connection;

    public void SetSource(string value)
    {
        Selected.Source = value;
        UpdateImages();
        var first = Images.FirstOrDefault()?.Name;

        Selected.Image = first switch
        {
            null => string.Empty,
            Constants.CycleMenuPak => string.Empty,
            _ => first
        };
            
        this.RaisePropertyChanged(nameof(Selected));
        //NotifyChange();
    }

    public void SetImage(string image)
    {
        Selected.Image = image switch
        {
            null => string.Empty,
            Constants.CycleMenuPak => string.Empty,
            _ => image
        };
        this.RaisePropertyChanged(nameof(Selected));
        //NotifyChange();
    }

    public string AdaptorButtonText(AdaptorSettings settings)
    {
        return settings.State switch
        {
            ServiceShould.Run => "Stop Adaptor",
            _ => "Start Adaptor"
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

    public string[] SourceNames => Sources.All().Select(f => f.Name).ToArray();

    public ObservableCollection<AvailableImage> Images { get; } = new ObservableCollection<AvailableImage>();

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
        this.RaisePropertyChanged(nameof(SourceNames));
        this.RaisePropertyChanged(nameof(Connections));
    }

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

    public ViewModelActivator Activator { get; }

    public void SetFilesPath(string path)
    {
        Files.SetRootDirectory(Selected, path);
    }
    
}
