using Gry;
using Gry.Serialization;
using Microsoft.Extensions.Logging;
using Nabu.Logs;
using Napa;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace Nabu.Cli.Packages
{
    public class ListCreate : PackageCommand<ListCreate.Settings>
    {
        YAMLSerializer Yaml { get; } = new();
        public ListCreate(IPackageManager packages, ILogger<ListCreate> log) : base(packages)
        {
            Log = log;
        }

        public ILogger<ListCreate> Log { get; }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var pattern = settings.Recurse switch
            {
                true => "**/*.napa",
                false => "*.napa"
            };

            var paths = Directories.List(settings.Path, pattern);
            var manifests = new List<Package>();
            Log.LogInformation($"Input path: {settings.Path}");
            Log.LogInformation($"Destination: {settings.Destination ?? settings.Path}");
            Log.LogInformation("Found Packages:");

            foreach (var path in paths)
            {
                Log.LogInformation(path);
                try
                {
                    var relativePath = Path.GetRelativePath(settings.Path, path);
                    var (_, package, _) = await Packages.Open(path);

                    if (package is null)
                    {
                        AnsiConsole.MarkupLine($"No manifest found in {path}, or it's invalid");
                        return -1;
                    }
                    manifests.Add(new SourcePackage(package, null, relativePath));
                }
                catch (Exception ex)
                {
                    NabuCli.WriteError(ex);
                }
            }
            Log.LogInformation("Exporting package list");
            var list = Yaml.Serialize(new SerializerOptions(), manifests.ToArray());
            await File.WriteAllTextAsync(
                Path.Combine(settings.Destination ?? settings.Path, "repo.yaml"), 
                list.ReadToEnd()
            );
            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandArgument(1, "[destination]")]
            public string? Destination { get; set; }

            [CommandArgument(0, "[path]")]
            public string Path { get; set; } = Environment.CurrentDirectory;

            [CommandOption("-r|--recurse")]
            public bool Recurse { get; set; } = false;
        }
    }
}