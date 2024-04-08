using System.ComponentModel;
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
{    bool readOnly = false;    public bool ReadOnly    {        get => readOnly;        set        {            if (readOnly != value)            {                readOnly = value;                OnPropertyChanged(nameof(ReadOnly));            }        }    }
    public object? Value
    {
        get => GetValue(Context);
        set
        {            var current = GetValue(Context);
            if (current is null || current != value)            {                SetValue(Context, value);                Change();            }
        }
    }

    public string? StringValue
    {
        get => (string?)GetValue(Context);
        set
        {            var current = GetValue(Context);            if (current is null || current is string s && s != value)            {                SetValue(Context, value);                Change();            }
        }
    }

    public bool? BoolValue
    {
        get => (bool?)GetValue(Context);
        set
        {            var current = GetValue(Context);            if (current is null || current is bool b && b != value)            {                SetValue(Context, value);                Change();            }
        }
    }

    public int? IntValue
    {
        get => (int?)GetValue(Context);
        set
        {            var current = GetValue(Context);            if (current is null || current is int i && i != value)            {                SetValue(Context, value);                Change();            }
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
        OnPropertyChanged(nameof(propertyName));        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(Changed));
    }    public void Reset()    {        Changed = false;        OnPropertyChanged(nameof(Value));        OnPropertyChanged(nameof(Changed));    }

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }    public void Refresh() => Change();

    public event PropertyChangedEventHandler? PropertyChanged;
};
