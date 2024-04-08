using Nabu.Settings;
using Spectre.Console.Cli;

namespace Nabu.Cli.Config;

public abstract class ConfigCommand<T> : AsyncCommand<T> where T : CommandSettings
{
    public ConfigCommand(GlobalSettings settings)
    {
        AppSettings = settings;
    }

    public GlobalSettings AppSettings { get; }
}