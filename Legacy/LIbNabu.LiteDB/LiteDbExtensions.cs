using Microsoft.Extensions.DependencyInjection;
using Nabu.Models;
using Nabu.Services;

namespace Nabu;

public static class LiteDbExtensions
{
    public static IServiceCollection AddLiteDb(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IRepository<>), typeof(LiteDbRepository<>));
        services.AddSingleton<ILiteDbModel<LogEntry>, LogEntryModel>();
        return services;
    }
}