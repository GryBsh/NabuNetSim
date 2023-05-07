using ReactiveUI;

namespace Nabu.NetSim.UI.ViewModels;

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
