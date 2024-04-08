﻿using Gry.Settings;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Nabu.NetSim.UI.Models;

public abstract record SettingsModel(string? Name, object? Source, object? Current) :
{
    public ObservableCollection<SettingValue>? Settings { get; protected set; }
    {
        foreach (var setting in Settings ?? [])
        {
            setting.SetValue(Source, setting.Value);
        }
    }
}