using Gry.Settings;
using System.ComponentModel;

namespace Nabu.NetSim.UI.Models;

public record SettingsModel<T> : SettingsModel
    where T : new()
{

    public SettingsModel(
        string? Name,
        T source,
        SettingsProvider settings,        Action<SettingValue,T?,T?>? onChange = null
    ) : base(Name, source, new T())
    {
        var sourceSettings = settings.Settings(Source).ToArray();
        Settings = new(settings.Settings(Current));
        var count = Settings.Count;
        for (var i = 0; i < count; i++)
        {
            Settings[i].SetValue(Settings[i].Context, sourceSettings[i].Value);            onChange?.Invoke(Settings[i], Source, Current);            Settings[i].PropertyChanged += (s, e) =>            {                if (e.PropertyName != nameof(SettingValue.Changed))                     return;                                var setting = s as SettingValue;                if (setting is not null)                    onChange?.Invoke(setting, Source, Current);                OnPropertyChanged(nameof(Changed));            };
        }
    }

    public new T? Source => (T?)base.Source;
    public new T? Current => (T?)base.Current;            }
