using Blazorise;
using DynamicData;
using Nabu.NetSim.UI.Models;
using Nabu.Network;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Nabu.Adaptor;

namespace Nabu.NetSim.UI.ViewModels;

public record AvailableImage(string DisplayName, string Name);

public class MenuViewModel : ReactiveObject
{
    public MenuViewModel(
        Settings settings,
        HomeViewModel home, 
        INabuNetwork sources, 
        //SettingsViewModel settingsModel,
        //StatusViewModel status,
        LogViewModel log
    )
    {
        Settings = settings;
        Home = home;
        Sources = sources;
        //Settings = settingsModel;
        //Status = status;
        Observable
            .Interval(TimeSpan.FromMinutes(1))
            .Subscribe(_ => UpdateImages());

        Task.Run(UpdateImages);
    }
    public Settings Settings { get; }
    //public SettingsViewModel Settings { get; }
    public HomeViewModel Home { get;  }
    public INabuNetwork Sources { get; }
    //public StatusViewModel Status { get; } 

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
        UpdateImages();
        SetVisible(MenuPage.AdaptorSettings);
        this.RaisePropertyChanged(nameof(AdaptorSelected));
        this.RaisePropertyChanged(nameof(Selected));
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

    public string[] SourceNames => Settings.Sources.Select(f => f.Name).ToArray();

    public ObservableCollection<AvailableImage> Images { get; } = new ObservableCollection<AvailableImage>();

    public void UpdateImages()
    {
        var images = Selected switch
        {
            NullAdaptorSettings => Array.Empty<AvailableImage>(),
            _ => Sources.Programs(Selected).Select(p => new AvailableImage(p.DisplayName, p.Name))
        };
        Images.Clear();
        Images.AddRange(images);
        this.RaisePropertyChanged(nameof(Images));
    }

    public bool HasMultipleImages
    {
        get
        {
            if (Selected is null or NullAdaptorSettings) return false;
            var programs = Sources.Programs(Selected);
            var hasMultipleImages = programs.Count() > 1;
            var exploitEnabled = Sources.Source(Selected)?.EnableExploitLoader is true;
            var isNotPakCycle = !programs.Any(p => p.Name == Constants.CycleMenuPak);
            return hasMultipleImages && (exploitEnabled || isNotPakCycle);
        }
    }

    public MenuPage Current { get; set; } = MenuPage.MainMenu;
    public Visibility IsVisible(MenuPage page)
    { 
        return Current == page ? Visibility.Visible : Visibility.Invisible;
    }
    public void SetVisible(MenuPage page)
    { 
        Current = page;
        if (Current is MenuPage.MainMenu)
        {
            Selected = new NullAdaptorSettings();
        }
        //NotifyChange();
    }
}
