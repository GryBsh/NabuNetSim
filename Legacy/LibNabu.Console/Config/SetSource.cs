using Nabu.Settings;
using Spectre.Console.Cli;

namespace Nabu.Cli.Config;

public class SetSource : ConfigCommand<SetSource.Settings>
{
    public SetSource(GlobalSettings settings) : base(settings)
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