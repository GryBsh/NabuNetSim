using Spectre.Console.Cli;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Nabu.Cli;


public class BuiltInCommandHandler
{
    public BuiltInCommandHandler() { }
    public Action Exit { get; set; } = () => { };
}

public static class NabuCli
{
    public static CommandApp CreateApp(IServiceCollection registrations)
    {
        var registrar = new TypeRegistrar(registrations);
        var app = new CommandApp(registrar);
        app.Configure(
            c =>
            {
                c.AddBranch(
                    "package",
                    package => package.AddCommand<NewPackageCommand>("new")
                );
            }
        );
        return app;
    }
}

public class CommandProcessor
{


    public static void HandleBuiltInCommands(string command, BuiltInCommandHandler handlers)
    {
        var cmd = command.ToLowerInvariant();
        if (BuiltInCommands.List.Contains(cmd) is false) return;

        Action? handler = cmd switch
        {
            BuiltInCommands.Exit => () => handlers.Exit(),
            _ => null
        };

        handler?.Invoke();
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
                AnsiConsole.MarkupLine(Markup.Error(result, "Exit Code"));
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(Markup.Error(ex.Message));
        }
        return result;
    }
}
