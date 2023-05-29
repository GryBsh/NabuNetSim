
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

var handlers = new BuiltInCommandHandler
{
    Exit = () =>
    {
        CancelSource.Cancel();
    }
};



var app = NabuCli.CreateApp(registrations);
if (args.Length > 0)
    return CommandProcessor.Execute(app, args);

while (CancelSource.IsCancellationRequested is false)
{
    var command = AnsiConsole.Prompt(
        new TextPrompt<string>("NABU>")
    );

    CommandProcessor.HandleBuiltInCommands(command, handlers);
    if (CancelSource.IsCancellationRequested)
        break;
    CommandProcessor.RunCommand(app, command);

}
return 0;

