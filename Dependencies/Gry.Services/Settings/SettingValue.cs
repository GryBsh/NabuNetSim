﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Gry.Settings;

public record SettingValue(
    string Name,
    string Label,
    string Section,
    string Description,
    Type Type,
    Func<object?, object?> GetValue,
    Action<object?, object?> SetValue,
    object Context,
    SettingValueType OptionsType,
    bool Advanced = false
) : INotifyPropertyChanged
{
    public object? Value
    {
        get => GetValue(Context);
        set
        {
            if (current is null || current != value)
        }
    }

    public string? StringValue
    {
        get => (string?)GetValue(Context);
        set
        {
        }
    }

    public bool? BoolValue
    {
        get => (bool?)GetValue(Context);
        set
        {
        }
    }

    public int? IntValue
    {
        get => (int?)GetValue(Context);
        set
        {
        }
    }

    bool changed = false;
    public bool Changed
    {
        get => changed;
        private set
        {
            if (changed != value)
            {
                changed = value;
            }
        }
    }

    void Change([CallerMemberName] string? propertyName = null)
    {
        Changed = true;
        OnPropertyChanged(nameof(propertyName));
        OnPropertyChanged(nameof(Changed));
    }

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
};