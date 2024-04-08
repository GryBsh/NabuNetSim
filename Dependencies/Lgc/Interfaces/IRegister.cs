using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Text;

namespace Lgc;

/// <summary>
/// Defines a method which, when called during container creation,
/// makes alternations to the SeviceCollection of the IHostBuilder.
/// </summary>
public interface IRegister
{
    void Register(IServiceCollection services, IConfiguration configuration);
}