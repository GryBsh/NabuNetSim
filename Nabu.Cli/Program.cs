

// Create a type registrar and register any dependencies.
// A type registrar is an adapter for a DI framework.
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

var registrations = new ServiceCollection();



// Create a new command app with the registrar
// and run it with the provided arguments.
//var app = new CommandApp<DefaultCommand>(registrar);

CancellationTokenSource CancelSource = new();

while (CancelSource.IsCancellationRequested is false) {
    Console.Write("NABU> ");
    var command = Console.ReadLine();
}


