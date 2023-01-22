using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu;
using Nabu.Adaptor;
using Nabu.Network;
using NLog.Extensions.Logging;

await   Host
        .CreateDefaultBuilder(args)
        .ConfigureServices(
            (context, services) => {
                var settings = new Settings();
                context.Configuration.Bind("Settings", settings);
                services.AddSingleton(settings);
                services.AddSingleton(settings.Sources);
                services.AddLogging(
                    logging =>
                        logging.ClearProviders().AddNLog("nlog.config")
                );
                
                services.AddHttpClient();
                Emulator.Register(services)
                        .AddTransient<IProtocol, NHACPProtocol>()
                        .AddTransient<IProtocol, RetroNetTelnetProtocol>()
                        .AddHostedService<Simulation>(); 
            }
        ).RunConsoleAsync();
