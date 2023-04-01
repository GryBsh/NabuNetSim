

// Create a type registrar and register any dependencies.
// A type registrar is an adapter for a DI framework.
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

//var registrations = new ServiceCollection();
//var app = new CommandApp<>(registrar);

CancellationTokenSource CancelSource = new();

while (CancelSource.IsCancellationRequested is false) {
    var command = AnsiConsole.Prompt(new TextPrompt<string>("NABU>"));

    if (command.ToLowerInvariant() is BuiltInCommands.ExitCommand)
    {
        CancelSource.Cancel();
    }
}


static class BuiltInCommands
{
    public const string ExitCommand = "exit";
}