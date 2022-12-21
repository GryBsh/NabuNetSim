using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Network;
using Nabu.Services;

var builder = Host.CreateDefaultBuilder(args);


builder.ConfigureServices(
    (context, services) =>
    {
        T AddSettings<T>(
            HostBuilderContext context,
            IServiceCollection services,
            string section
        ) where T : class, new()
        {
            T settings = new();
            context.Configuration.Bind(section, settings);
            services.AddSingleton(settings);
            return settings;
        }

        var settings = AddSettings<Settings>(context, services, "Settings");
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
        services.AddTransient<SerialAdaptorEmulator>();
        services.AddTransient<TCPAdaptorEmulator>();
        services.AddTransient<NetworkEmulator>();
        services.AddHostedService<EmulatorService>();
    }
);



//var tokenSource = new CancellationTokenSource();
await builder.RunConsoleAsync();
