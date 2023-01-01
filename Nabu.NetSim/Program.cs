using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu;
using Nabu.Adaptor;
using Nabu.Network;


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
                        logging.AddSimpleConsole(
                            options =>
                            {
                                options.IncludeScopes = true;
                                options.SingleLine = true;
                                options.TimestampFormat = "HH:mm:ss ";
                            }
                        )
                );
                services.AddHttpClient();
                services.AddTransient<NabuNetProtocolService>();
                services.AddTransient<NabuNetProtocol>();
                services.AddTransient<IProtocol, ACPProtocol>();
                services.AddHostedService<Simulation>(); 
            }
        ).RunConsoleAsync();
