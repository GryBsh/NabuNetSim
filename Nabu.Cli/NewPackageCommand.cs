using Napa;
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
            [CommandArgument(0, "[path]")]
            public string Path { get; set; } = Environment.CurrentDirectory;
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            return 0;
        }
    }

    

    public class NewPackageCommand : Command<NewPackageCommand.Settings>
    {
        public IPackageManager Packages { get; }

        public class Settings : CommandSettings
        {
            [CommandArgument(0, "[path]")]
            public string Path { get; set; } = Environment.CurrentDirectory;

            [CommandArgument(1, "[destination]")]
            public string Destination { get; set; } = Environment.CurrentDirectory;
        }

        public NewPackageCommand(IPackageManager packages)
        {
            Packages = packages;
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (settings.Path is null)
            {
                AnsiConsole.Markup(Markup.Error(nameof(settings.Path), "argument null"));
                return -1;
            }

            var directories = Directory.Exists(settings.Path) switch
            {
                true => Directory.GetDirectories(settings.Path),
                false => new[] { settings.Path }
            };

            foreach (var directory in directories)
            {

                var (_, manifest) = Packages.Open(directory).GetAwaiter().GetResult();

                if (manifest is null)
                {
                    AnsiConsole.WriteLine($"No manifest found in {directory}, or it's invalid");
                    return -1;
                }
            }

            return 0;
        }
    }
}
