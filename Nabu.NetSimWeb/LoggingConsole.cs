﻿using Microsoft.Extensions.Logging;
using Nabu.NetSim.UI;
using Nabu.NetSim.UI.Services;
using Nabu.Services;
using System.Xml.Linq;
using System;
using Nabu.NetSim.UI.Models;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.EntityFrameworkCore;

namespace Nabu.NetSimWeb
{
    

    public class LoggingConsole<T> : IConsole<T>
    {
        ILogger Logger { get; }
        //public IServiceScopeFactory ScopeFactory { get; }
        string Name { get; } = typeof(T).Name;
        public LoggingConsole(
            ILogger<T> logger
        )
        {
            Logger = logger;
            //ScopeFactory = scopeFactory;
            
        }
        /*
        void Log(LogLevel level, string message)
        {
            using var scope = ScopeFactory.CreateScope();
            var logs = scope.ServiceProvider.GetRequiredService<LogService>();
            logs.Insert(
                new LogEntry(
                    Guid.NewGuid(),
                    DateTime.Now,
                    level,
                    Name,
                    message
                )
            );
        }
        */

        public void Write(string message)
        {
            Logger.LogInformation(message);
            //Log(LogLevel.Information, message);
            
        }

        public void WriteError(string message, Exception? exception = null)
        {
            Logger.LogError(exception, message);
            //Log(LogLevel.Error, message);
        }

        public void WriteVerbose(string message)
        {
            Logger.LogDebug(message);
            //Log(LogLevel.Debug, message);
        }

        public void WriteWarning(string message)
        {
            Logger.LogWarning(message);
            //Log(LogLevel.Warning, message);
        }

        public void WriteWarning(string message, Exception? exception = null)
        {
            Logger.LogWarning(exception, message);
            //Log(LogLevel.Warning, message);
        }
    }
}
