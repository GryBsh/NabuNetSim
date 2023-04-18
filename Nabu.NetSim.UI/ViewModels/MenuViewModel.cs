using Blazorise;
using Nabu.Network;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels;

public enum MenuPage
{
    MainMenu,
    Adaptors,
    Settings,
    Connections,
    AdaptorSettings,
    Storage,
    Features,
    MAME,
    CLI
}

public class SettingsViewModel : ReactiveObject
{
    Settings Settings { get; }
    public SettingsViewModel(Settings settings)
    {
        Settings = settings;
    }

    public bool EnableLocalFileCache
    {
        get => Settings.EnableLocalFileCache; 
        set => Settings.EnableLocalFileCache = value;
    }

    public bool EnablePython
    {
        get => Settings.EnablePython; 
        set => Settings.EnablePython = value;
    }
    public bool EnableJavaScript
    {
        get => Settings.EnableJavaScript; 
        set => Settings.EnableJavaScript = value;
    }
}
public class MenuViewModel : ReactiveObject
{
    public MenuViewModel(HomeViewModel home, INabuNetwork sources)
    {
        Home = home;
        Sources = sources;
        Settings = new (home.Settings);
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
        this.RaisePropertyChanged(nameof(AdaptorSelected));
        NotifyChange();
        SetVisible(MenuPage.AdaptorSettings);
    }

    public void SetSource(string value)
    {
        Selected.Source = value;
        Selected.Image = string.Empty;
        this.RaisePropertyChanged(nameof(Selected.Image));
    }

    public string AdaptorButtonText(AdaptorSettings settings)
    {
        return settings.Next switch
        {
            ServiceShould.Run => "Stop Adaptor",
            _ => "Start Adaptor"
        };
    }

    public IEnumerable<(string, string)> AvailableImages()
    {
        if (Selected is NullAdaptorSettings) 
            return Array.Empty<(string, string)>();

        return Sources.Programs(Selected).Select(p => (p.DisplayName, p.Name));
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
