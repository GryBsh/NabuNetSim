using Microsoft.Extensions.Logging;
using Nabu.Logs;
using Napa;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace Nabu.Cli.Packages
{
    public class PackageCreate : PackageCommand<PackageCreate.Settings>
    {
        public PackageCreate(IPackageManager packages, ILogger<PackageCreate> log) : base(packages)
        {
            Log = log;
        }

        public ILogger<PackageCreate> Log { get; }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var paths = Directory.Exists(settings.Path) switch
            {
                true => Directory.GetDirectories(settings.Path),
                false => new[] { settings.Path }
            };

            foreach (var path in paths)
            {
                Log.LogInformation($"Creating from {path}");
                try
                {
                    var (_, manifest, _) = await Packages.Open(path);

                    if (manifest is null)
                    {
                        NabuCli.WriteError($"No manifest found in {path}, or it's invalid");
                        return -1;
                    }

                    await Packages.Create(path, settings.Destination);
                }
                catch (Exception ex)
                {
                    NabuCli.WriteError(ex);
                }
            }

            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandArgument(1, "[destination]")]
            public string Destination { get; set; } = Environment.CurrentDirectory;

            [CommandArgument(0, "[path]")]
            public string Path { get; set; } = Environment.CurrentDirectory;
        }
    }
}