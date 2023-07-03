using Microsoft.Extensions.DependencyInjection;
using Nabu.Cli.Packages;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nabu.Cli;

public static class NabuCli
{
    public static CommandApp CreateApp(IServiceCollection registrations, Action<CommandApp?> appConfig, Action<IConfigurator>? commandConfig = null)
    {
        var registrar = new TypeRegistrar(registrations);
        var app = new CommandApp(registrar);
        
        appConfig(app);
        app.Configure(
            root =>
            {
                root.SetApplicationName("nns");
                root.SetApplicationVersion(Emulator.Version);
                root.AddBranch(
                    "pkg",
                    package =>
                    {
                        package.AddCommand<PackageCreate>("new");
                        package.AddCommand<PackageInstall>("add");
                        package.AddBranch(
                            "list", 
                            list => list.AddCommand<ListCreate>("new")
                        );
                    }
                );
                commandConfig?.Invoke(root);
            }
        );
        return app;
    }
    
    public static void WriteError(Exception ex)
    {
        WriteError(ex.Message);
    }

    public static void WriteError(string message, string? header = null)
    {
        if (header is not null)
            AnsiConsole.MarkupLine(Markup.Error(message, header));
        else
            AnsiConsole.MarkupLine(Markup.Error(message));
    }

    public static (bool, string) HandleBuiltInCommands(string command, BuiltInCommandHandler handlers)
    {
        var cmd = command.ToLowerInvariant();
        if (BuiltInCommands.List.Contains(cmd) is false) 
            return (true, command);

        if (handlers.TryGetValue(cmd, out var handler))
            return handler.Invoke(command);

        return (true, command);
    }

    public static int RunCommand(CommandApp app, string command)
        => Execute(app, command.Split(' '));

    public static int Execute(CommandApp app, string[] command)
    {
        int result = -1;
        try
        {
            result = app.Run(command);
            if (result != 0 && result > -1)
            {
                AnsiConsole.MarkupLine(Markup.Error(result, "Exit"));
            }
        }
        catch (Exception ex)
        {
            WriteError(ex.Message);
        }
        return result;
    }
}