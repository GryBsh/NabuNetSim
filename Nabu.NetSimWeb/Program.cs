using Figgle;
using Gry.Serialization;
using Gry.Settings;
using Nabu;
using Nabu.Cli;
using Nabu.NetSimWeb;
using Nabu.Network;
using Nabu.Services;
using Nabu.Settings;
using ReactiveUI.Blazor;
using Spectre.Console;
using Spectre.Console.Cli;


AnsiConsole.Write(FiggleFonts.Rectangles.Render("NABU NetSim"));
AnsiConsole.WriteLine($"v{Emulator.Version} (c) 2022-{DateTime.Now.Year} Nick Daniels");
AnsiConsole.WriteLine();

CancellationTokenSource CancelSource = new();

var registrations = new ServiceCollection();
RegisterServices(registrations);
return NabuCli.Execute(App(registrations), args);

void RegisterServices(IServiceCollection services)
{
    
    services.AddHttpClient();
    services.AddTransient<ISerializeProvider, SerializerProvider>();
    services.AddTransient<ISerialize, YAMLSerializer>();
    services.AddTransient<ISerialize, JSONSerializer>();
    services.AddSingleton<SettingsProvider>();
    services.AddSingleton(sp =>
    {
        return sp.GetRequiredService<SettingsProvider>()
                 .FromSection<GlobalSettings>("Settings") ?? 
               new();
    });
}

CommandApp App(ServiceCollection registrations)
{
    return NabuCli.CreateApp(
    registrations,
    app => app?.SetDefaultCommand<ServerStart>(),
    root =>
        root.AddBranch(
            "server",
            server => server.AddCommand<ServerStart>("start")
        )
    );
}