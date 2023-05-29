using Nabu.NetSim.UI.Services;
using ReactiveUI;

namespace Nabu.NetSim.UI.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    Settings Settings { get; }
    LogService Logs { get; }

    public SettingsViewModel(Settings settings, LogService logs)
    {
        Settings = settings;
        Logs = logs;
    }

    public string LogRefreshMode
    {
        get => Logs.RefreshMode.ToString();
        set => Logs.RefreshMode = Enum.Parse<RefreshMode>(value, true);
    } 
  
}
