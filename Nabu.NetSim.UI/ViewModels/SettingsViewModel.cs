using Blazorise;
using DynamicData;
using Gry.Adapters;
using Gry.Serialization;
using Gry.Settings;
using Nabu.NetSim.UI.Models;
using Nabu.Network;
using Nabu.Settings;
using Nabu.Sources;
using Napa;
using NLog.LayoutRenderers;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Nabu.NetSim.UI.ViewModels;


public class SettingsViewModel : ReactiveObject
{
    public AdaptorSettingsViewModel Menu { get; }
    public INabuNetwork Network { get; }
    public ISourceService Sources { get; }
    public IPackageManager PackageManager { get; }
    public SettingsProvider SettingsProvider { get; }
    public GlobalSettings Global { get; }
    public SettingsModel<GlobalSettings>? Value { get; set; }
    public GlobalSettings? Current => Value?.Current;
    public ObservableCollection<SettingValue>? Settings { get; set; }

    public ObservableCollection<SettingsModel<SerialAdaptorSettings>> SerialAdaptors { get; } = [];

    public ObservableCollection<SettingsModel<TCPAdaptorSettings>> TCPAdaptors { get; } = [];

    public static Dictionary<string, string> SectionIcons = new()
    {
        ["General"] = "fa-gear",
        ["Network"] = "fa-network",
        ["Plugins"] = "fa-plug",
        ["Sources"] = "fa-database",
        ["Headless"] = "fa-window-maximize",
        ["Logs"] = "fa-file",
        ["Storage"] = "fa-save",
        ["Packages"] = "fa-box"
    };

    public IEnumerable<(string, string, SettingValue[])>? Sections
        => Settings?.Where(s => ShowAdvanced || !s.Advanced)
                    .GroupBy(s => s.Section)
                    .OrderBy(g => g.Key)
                    .Select(g => (g.Key, SectionIcons[g.Key], g.ToArray()));

    public bool ShowAdvanced { get; set; } = false;


    public InstalledPackage? Package(string? id)
        => PackageManager.Installed.FirstOrDefault(p => p.Id == id);

    public ObservableCollection<NabuProgram> Programs { get; set; } = [];

    public SettingsViewModel(
        GlobalSettings global, 
        AdaptorSettingsViewModel menu, 
        SettingsProvider settingsProvider,
        HomeViewModel home,
        INabuNetwork network, 
        ISourceService sources, 
        IPackageManager packageManager
    )
    {
        Global = global;
        
        Menu = menu;
        SettingsProvider = settingsProvider;
        Network = network;
        Sources = sources;
        PackageManager = packageManager;
        Revert();
        home.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(home.VisiblePage))
                if (home.IsVisible(VisiblePage.Settings) is Visibility.Visible)
                    Revert();                                                                               
        };
    }

    static void DisposeOfRemoved<TAdaptor>(ICollection<SettingsModel<TAdaptor>> items, ICollection<TAdaptor> missingIn)
        where TAdaptor : AdaptorSettings, new()
    {
        foreach (
            var (setting, removed) in
            from model in items.ToArray()
            from adapter in missingIn.ToArray()
            where model.Source != adapter
            select (model, model.Source)
        )
        {
            removed.Adapter?.Dispose();
            items.Remove(setting);
        }
    }


    static void DisposeOfRemoved<TAdaptor>(ICollection<TAdaptor> items, ICollection<SettingsModel<TAdaptor>> missingIn)
        where TAdaptor : AdaptorSettings, new()
    {
        foreach (
            var removed in
            from adapter in items.ToArray()
            from model in missingIn.ToArray()
            where model.Source != adapter
            select adapter
        )
        {
            removed.Adapter?.Dispose();
            items.Remove(removed);
        }
    }

    void AdaptorSettingsChanged(SettingValue setting, AdaptorSettings? source, AdaptorSettings? current)
    {
        if (setting.Name == nameof(AdaptorSettings.Port) && source?.Enabled is true)
            setting.ReadOnly = true;

        this.RaisePropertyChanged(nameof(HasChanged));
        this.RaisePropertyChanged(nameof(CanSave));

    }

    void InitAdded<TAdaptor>(IEnumerable<TAdaptor> items, ICollection<SettingsModel<TAdaptor>> missingIn, SettingsProvider provider)
        where TAdaptor : AdaptorSettings, new()
    {
        foreach (
            var added in
            from item in items
            where !missingIn.Any(s => s.Current?.Port == item.Port)
            select item
        )   missingIn.Add(
                new SettingsModel<TAdaptor>(added.Port, added, provider, AdaptorSettingsChanged)
            );
    }

    public void Save()
    {
        Apply();
        SettingsProvider.SaveSettings("Settings", Global);
        foreach (var adapter in Global.Adapters)
        {
            adapter.ResetChanged();
        }        AppliedButNotSaved = false;
    }

    public bool HasChanged 
        =>  Value?.Changed is true || 
            SerialAdaptors.Any(s => s.Changed) ||
            TCPAdaptors.Any(t => t.Changed);
    
    public bool CanSave
        => HasChanged || Global.Adapters.Any(a => a.ChangedSinceLoad) || AppliedButNotSaved;
    public bool AppliedButNotSaved { get; set; }
    public void Apply()
    {
        Value?.Apply();


        DisposeOfRemoved(Global.Serial, SerialAdaptors);
        DisposeOfRemoved(Global.TCP, TCPAdaptors);


        foreach (var (settings,adapter) in from serial in SerialAdaptors select (serial, serial.Source))
        {
            settings.Apply();
            
            if (!Global.Serial.Any(s => s.Port == adapter.Port))
                Global.Serial.Add(adapter);
        }

        foreach (var (settings, adapter) in from tcp in TCPAdaptors select (tcp, tcp.Source))
        {
            settings.Apply();

            if (!Global.TCP.Any(t => t.Port == adapter.Port))
                Global.TCP.Add(adapter);
        }
        AppliedButNotSaved = true;
    }

    void SettingsChanged(SettingValue setting, GlobalSettings? source, GlobalSettings? current)
    {
        this.RaisePropertyChanged(nameof(HasChanged));
        this.RaisePropertyChanged(nameof(CanSave));
    }

    public void Revert()
    {

        DisposeOfRemoved(SerialAdaptors, Global.Serial);
        DisposeOfRemoved(TCPAdaptors, Global.TCP);

        Value = new SettingsModel<GlobalSettings>("Global Settings", Global, SettingsProvider, SettingsChanged);

        Current!.Serial = new(Global.Serial);
        Current!.TCP = new(Global.TCP);

        Settings = Value.Settings;
        

        SerialAdaptors.Clear();
        TCPAdaptors.Clear();

        SetSelected(new NullAdaptorSettings(), true);

        InitAdded(Current?.Serial ?? [], SerialAdaptors, SettingsProvider);
        InitAdded(Current?.TCP ?? [], TCPAdaptors, SettingsProvider);
        this.RaisePropertyChanged(nameof(Value));        Value.PropertyChanged += (_, e) =>        {            Task.Run(RefreshPrograms);        };                                                                                                                       RefreshPrograms();
        
        this.RaisePropertyChanged(nameof(Settings));
        this.RaisePropertyChanged(nameof(SerialAdaptors));
        this.RaisePropertyChanged(nameof(TCPAdaptors));

    }
    void RefreshPrograms()
    {
        Programs = new(Value?.Current switch
        {
            null => [],
            _ => Network.Programs(
                     Value.Current.HeadlessSource
                 ).Where(p => p.Headless)                 .ToArray()
        });

        this.RaisePropertyChanged(nameof(Programs));
    }


    public void NewSerialAdapter()
    {
        var adaptor = new SerialAdaptorSettings();
        SetSelected(adaptor);
        SerialAdaptors.Add(
            new SettingsModel<SerialAdaptorSettings>(adaptor.Port, adaptor, SettingsProvider, AdaptorSettingsChanged)
        );
        Current?.Serial.Add(adaptor);
    }

    public void NewTCPAdapter()
    {
        var adaptor = new TCPAdaptorSettings();
        var settings = new SettingsModel<TCPAdaptorSettings>(adaptor.Port, adaptor, SettingsProvider, AdaptorSettingsChanged);

        SetSelected(adaptor);
        adaptor.PropertyChanged += (s, e) =>
        {
            TCPAdaptors.Remove(settings);
            adaptor = new TCPAdaptorSettings();
            settings = new SettingsModel<TCPAdaptorSettings>(adaptor.Port, adaptor, SettingsProvider, AdaptorSettingsChanged);

        };
        TCPAdaptors.Add(
            settings     
        ); 
        Current?.TCP.Add(adaptor);
    }

   

    public void RemoveSerialAdaptor(
        SettingsModel<SerialAdaptorSettings> adaptor
    )
    {
        if (Menu.Selected == adaptor.Current)
            SetSelected(new NullAdaptorSettings(), true);

        SerialAdaptors.Remove(adaptor);
        if (adaptor.Source is not null && Current!.Serial.Contains(adaptor.Source))
        {
            Current!.Serial?.Remove(adaptor.Source);
        }
    }

    public void RemoveTCPAdaptor(SettingsModel<TCPAdaptorSettings> adaptor)
    {
        if (Menu.Selected == adaptor.Current)
            SetSelected(new NullAdaptorSettings(), true);

        TCPAdaptors.Remove(adaptor);
        if (adaptor.Source is not null && Current!.TCP.Contains(adaptor.Source))
        {
            Current!.TCP.Remove(adaptor.Source);
        }

            
    }


    public void SetSelected<TAdapter>(SettingsModel<TAdapter> adaptor, bool force = false)
        where TAdapter : AdaptorSettings, new()
    {
        
        Menu.SetSelected(adaptor);
        
        //this.RaisePropertyChanged(nameof(Settings));
    }


    public void SetSelected(AdaptorSettings adaptor, bool force = false)
    {
        
        Menu.SetSelected(adaptor);
        
        //this.RaisePropertyChanged(nameof(Settings));
    }

    public bool IsSelected(AdaptorSettings adaptor)
        => Menu.Selected == adaptor;
    
}