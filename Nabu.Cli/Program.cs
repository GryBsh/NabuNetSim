
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;


var registrations = new ServiceCollection();



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