using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Cli.Config;

public class SetSource : ConfigCommand<SetSource.Settings>
{
    public SetSource(Nabu.Settings settings) : base(settings)
    {
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        return Task.FromResult(0);
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "-a|--adaptor")]
        public int? Adaptor { get; set; }
    }
}