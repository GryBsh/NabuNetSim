using Microsoft.Extensions.Hosting;
using System;

namespace Lgc;

/// <summary>
/// Defines a transient auto-discovered Service Type
/// </summary>
public interface IDependency
{ }

/// <summary>
/// Defines an auto-discovered hosted service
/// </summary>
public interface IService : IHostedService, ISingletonDependency
{ }

/// <summary>
/// Defines a scoped auto-discovered Service Type
/// </summary>
public interface IScopedDependency : IDependency
{ }

/// <summary>
/// Defines a singleton auto-discovered Service Type
/// </summary>
public interface ISingletonDependency : IDependency
{ }

/// <summary>
/// Defines auto-discovered/bound configuration
/// </summary>
public interface IDependencyOptions
{ }