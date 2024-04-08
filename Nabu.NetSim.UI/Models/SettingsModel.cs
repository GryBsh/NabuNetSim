using Gry.Settings;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Nabu.NetSim.UI.Models;

public abstract record SettingsModel(string? Name, object? Source, object? Current) :    INotifyPropertyChanged
{
    public ObservableCollection<SettingValue>? Settings { get; protected set; }    public bool Changed => Settings?.Any(s => s.Changed) is true;    public void Apply()
    {
        foreach (var setting in Settings ?? [])
        {
            setting.SetValue(Source, setting.Value);            setting.Reset();
        }
    }    public void Revert()    {        foreach (var setting in Settings ?? [])        {            setting.Value = setting.GetValue(Source);            setting.Reset();        }    }    public event PropertyChangedEventHandler? PropertyChanged;    protected void OnPropertyChanged(string propertyName)    {        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));    }
}
