using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

public static class ScopeExtensions
{

    public static TService? Service<TService>(this IServiceScope scope)
        => scope.ServiceProvider
            .GetService<TService>();

    

    public static IEnumerable<TService> Services<TService>(this IServiceScope scope)
        => scope.ServiceProvider
            .GetServices<TService>();

    
}