using Microsoft.Extensions.Logging;
using System;

namespace Lgc.Hosting;/// <summary>/// Provides a null implementation of IServiceProvider./// </summary>[Runtime.Invisible]public class NullServiceProvider() : IServiceProvider{    public object? GetService(Type serviceType)    {        return null;    }}