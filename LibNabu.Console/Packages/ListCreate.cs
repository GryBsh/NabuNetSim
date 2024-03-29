﻿using Nabu.Services;
using Napa;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace Nabu.Cli.Packages
{
    public class ListCreate : PackageCommand<ListCreate.Settings>
    {
        public ListCreate(IPackageManager packages, ILog<ListCreate> log) : base(packages)
        {
            Log = log;
        }

        public ILog<ListCreate> Log { get; }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var pattern = settings.Recurse switch
            {
                true => "**/*.napa",
                false => "*.napa"
            };

            var paths = NabuLibEx.List(settings.Path, pattern);
            var manifests = new List<Package>();
            Log.Write($"Input path: {settings.Path}");
            Log.Write($"Destination: {settings.Destination ?? settings.Path}");
            Log.Write("Found Packages:");

            foreach (var path in paths)
            {
                Log.Write(path);
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
            Log.Write("Exporting package list");
            var list = Yaml.Serialize(manifests.ToArray());
            await File.WriteAllTextAsync(Path.Combine(settings.Destination ?? settings.Path, "repo.yaml"), list);
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