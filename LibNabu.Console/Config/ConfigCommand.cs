using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Cli.Config;

public abstract class ConfigCommand<T> : AsyncCommand<T> where T : CommandSettings
{
    public ConfigCommand(Settings settings)
    {
        AppSettings = settings;
    }

    public Settings AppSettings { get; }
}