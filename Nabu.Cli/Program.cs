
using Microsoft.Extensions.DependencyInjection;
using Nabu.Cli;
using Spectre.Console;
using Spectre.Console.Cli;

CancellationTokenSource CancelSource = new();

var exitCodeMessages = new Dictionary<int, string>()
{
    [-1] = "Unknown Failure"
};

var registrations = new ServiceCollection();

void HandleBuiltInCommands(string command)
{
    var cmd = command.ToLowerInvariant();
    if (BuiltInCommands.List.Contains(cmd) is false) return;

    Action? handler = cmd switch
    {
        BuiltInCommands.Exit => () => CancelSource.Cancel(),
        _ => null
    };

    handler?.Invoke();
}

CommandApp CreateApp(IServiceCollection registrations)
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

while (CancelSource.IsCancellationRequested is false) {
    var command = AnsiConsole.Prompt(
        new TextPrompt<string>("NABU>")
    );

    HandleBuiltInCommands(command);
    if (CancelSource.IsCancellationRequested) 
        break;

    var app = CreateApp(registrations);
    try
    {
        var result = app.Run(command.Split(' '));
        if (result != 0 && result > -1)
        {
            AnsiConsole.MarkupLine(Nabu.Cli.Markup.Error(result, "Exit Code"));
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine(Nabu.Cli.Markup.Error(ex.Message));
    }
    
}
