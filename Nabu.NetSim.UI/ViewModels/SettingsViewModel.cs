using Nabu.NetSim.UI.Services;
using Nabu.Services;
using ReactiveUI;

namespace Nabu.NetSim.UI.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private Settings Settings { get; }
    private ILogService Logs { get; }

    public SettingsViewModel(Settings settings, ILogService logs)
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