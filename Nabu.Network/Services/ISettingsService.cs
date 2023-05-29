using Nabu.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Services
{
    public interface ISettingsService
    {
        Task Open();
        EmulatorSettings? Current { get; }
    }

    public class SettingsService : ISettingsService
    {
        public EmulatorSettings? Current { get; private set; }
        IFileCache Cache { get; }
        IHttpCache Http { get; }
        string SettingsPath { get; } = Path.Combine(AppContext.BaseDirectory, "settings.yaml");
        public SettingsService(IFileCache cache, IHttpCache http) 
        {
            Cache = cache;
            Http = http;
        }

        public async Task Open()
        {
            Current = (
                await Yaml.Deserialize<EmulatorSettings>(SettingsPath, Cache, Http)
            )?.FirstOrDefault();
        }

        public async void Commit()
        {   
            if (Current is null) return;

            var serialized = Yaml.Serialize(Current);
            await File.WriteAllTextAsync(SettingsPath, serialized);
        }
    }

    public record EmulatorSettings : DeserializedObject
    {
        public LogSettings? Logs
        {
            get => Get<LogSettings>(nameof(Logs));
            set => Set(nameof(Logs), value);
        }

        public StorageSettings? Storage
        {
            get => Get<StorageSettings>(nameof(Storage));
            set => Set(nameof(Storage), value);
        }
        
        public DatabaseSettings? Database
        {
            get => Get<DatabaseSettings>(nameof(Database));
            set => Set(nameof(Database), value);
        }

        public ExtensionSettings? Extensions
        {
            get => Get<ExtensionSettings>(nameof(Extensions));
            set => Set(nameof(Extensions), value);
        }
    }

    public record StorageSettings : DeserializedObject
    { 
        public bool EnableFileCache
        {
            get => Get<bool>(nameof(EnableFileCache));
            set => Set(nameof(EnableFileCache), value);
        }
    }

    public record DatabaseSettings : DeserializedObject
    {
        public string? FilePath
        {
            get => Get<string>(nameof(FilePath));
            set => Set(nameof(FilePath), value);
        }
    }

    public record LogSettings : DeserializedObject
    {
        public int MaxEntriesPerPage { 
            get => Get<int>(nameof(MaxEntriesPerPage)); 
            set => Set(nameof(MaxEntriesPerPage), value);
        }

        public int MaxAgeInDays
        {
            get => Get<int>(nameof(MaxAgeInDays));
            set => Set(nameof(MaxAgeInDays), value);
        }

    }

    public record ExtensionSettings : DeserializedObject
    {
        public List<ProtocolSettings> Protocols
        {
            get => Get<List<ProtocolSettings>>(nameof(Protocols)) ?? new();
            set => Set(nameof(Protocols), value);
        }

        public bool EnableJavaScript
        {
            get => Get<bool>(nameof(EnableJavaScript));
            set => Set(nameof(EnableJavaScript), value);
        }

    }
}
