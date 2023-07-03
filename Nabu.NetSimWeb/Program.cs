using Figgle;
using Nabu;
using Nabu.Cli;
using Nabu.NetSimWeb;
using Nabu.Network;
using Nabu.Services;
using ReactiveUI.Blazor;
using Spectre.Console;
using Spectre.Console.Cli;

var settings = new Settings();

AnsiConsole.Write(FiggleFonts.Rectangles.Render("NABU NetSim"));
AnsiConsole.WriteLine($"v{Emulator.Version} (c) 2022-{DateTime.Now.Year} Nick Daniels");
AnsiConsole.WriteLine();

if (args.Length is 0)
    return ServerStart.WebServer(settings, args);

CancellationTokenSource CancelSource = new();
var handlers = new BuiltInCommandHandler(CancelSource);

var registrations = new ServiceCollection();

ServerStart.AddNabu(registrations, settings);
registrations.AddSingleton(settings);
registrations.AddHttpClient();

var app = NabuCli.CreateApp(
    registrations,
    app => app?.SetDefaultCommand<ServerStart>(),
    root =>
        root.AddBranch(
            "server",
            server => server.AddCommand<ServerStart>("start")
        )
);
return NabuCli.Execute(app, args);