using Napa;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nabu.Cli.Packages
{
    public class PackageInstall : PackageCommand<PackageInstall.Settings>
    {
        public PackageInstall(IPackageManager packages) : base(packages)
        {
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            settings.Path = settings.Path.Trim('\"');

            var (path, package, _) = await Packages.Open(settings.Path);

            AnsiConsole.MarkupLine($"[yellow]Name:[/] {package.Name}");
            AnsiConsole.MarkupLine($"[yellow]Version:[/] {package.Version}");
            AnsiConsole.MarkupLine($"[yellow]Author:[/] {package.Author}");

            if (!string.IsNullOrWhiteSpace(package.Url))
                AnsiConsole.MarkupLine($"[yellow]Url:[/] {package.Url}");

            if (AnsiConsole.Confirm("Install Package?", false))
                await Packages.Install(settings.Path);

            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandArgument(0, "[path]")]
            public string Path { get; set; } = Environment.CurrentDirectory;
        }
    }
}