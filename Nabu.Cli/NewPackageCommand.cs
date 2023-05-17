using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace Nabu.Cli
{
    public class NewSourceList : Command<NewSourceList.Settings>
    {
        public class Settings : CommandSettings
        {

        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            return 0;
        }
    }

    

    public class NewPackageCommand : Command<NewPackageCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandArgument(0, "[path]")]
            public string? Path { get; set; }

            [CommandArgument(1, "[destination]")]
            public string Destination { get; set; } = Environment.CurrentDirectory;
        }

        public NewPackageCommand() { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (settings.Path is null || Path.Exists(settings.Path) is false)
            {
                AnsiConsole.Markup(Markup.Error($"Path `{settings.Path}` not found."));
                return -1;
            }

            var napaFile = Path.Combine(settings.Path, "napa.yaml");
            if (Path.Exists(napaFile) is false)
            {
                AnsiConsole.Markup(Markup.Error($"NAPA file not found."));
            }

            var napaArchivePath = Path.Combine(settings.Destination, $"{Path.GetFileName(settings.Path)}.napa");

            ZipFile.CreateFromDirectory(settings.Path, napaArchivePath);

            return 0;
        }
    }
}
