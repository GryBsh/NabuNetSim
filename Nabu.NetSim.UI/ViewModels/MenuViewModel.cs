using Blazorise;
using DynamicData;
using Nabu.Network;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels;

public record AvailableImage(string DisplayName, string Name);

public class AdaptorViewModel : ReactiveObject
{
    HomeViewModel Home;
    INabuNetwork Sources;
    Settings Settings;
    public AdaptorViewModel(HomeViewModel home, INabuNetwork sources, Settings settings)
    {
        Home = home;
        Sources = sources;
        Settings = settings;
    }

    public ObservableCollection<AvailableImage> AvailableImages { get; } = new();

}

public class MenuViewModel : ReactiveObject
{
    public MenuViewModel(HomeViewModel home, INabuNetwork sources)
    {
        Home = home;
        Sources = sources;
        Settings = new (home.Settings);
        Observable.Interval(TimeSpan.FromMinutes(1))
            .Subscribe(_ => UpdateImages());
        UpdateImages();
    }

    public SettingsViewModel Settings { get; }
    public HomeViewModel Home { get;  }
    public INabuNetwork Sources { get; }

    void NotifyChange()
    {
        Home.RaisePropertyChanged(nameof(Home.Menu));
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
        NotifyChange();
    }

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
        NotifyChange();
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
        NotifyChange();
    }

    public string AdaptorButtonText(AdaptorSettings settings)
    {
        return settings.State switch
        {
            ServiceShould.Run => "Stop Adaptor",
            _ => "Start Adaptor"
        };
    }

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
        NotifyChange();
    }
}
