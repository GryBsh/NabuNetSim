using Gry;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Nabu.Cli;

public class AnsiLogger : DisposableBase, ILogger
{
    private readonly string FullName;
    private readonly string Name;

    public AnsiLogger(
        string name
    )
    {
        FullName = name;
        Name = name.Split('.')[^1];
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => default!;

    public bool IsEnabled(LogLevel logLevel)
        => //logLevel >= LogLevel.Information &&
           (logLevel <= LogLevel.Information && (FullName.StartsWith("Microsoft") is false || FullName.StartsWith("Microsoft.Hosting") is true)) &&
           (logLevel <= LogLevel.Information && FullName.StartsWith("System.Net.Http") is false);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        Func<object, string?, string> markup = logLevel switch
        {
            LogLevel.Information => Markup.Info,
            LogLevel.Error => Markup.Error,
            _ => Markup.Warning,
        };

        var header = Header();
        var message = formatter(state, exception);

        //var textColor = Console.ForegroundColor;
        /*
        var levelColor = logLevel switch
        {
            LogLevel.Information => ConsoleColor.Green,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.Yellow
        };

        Console.ForegroundColor = levelColor;
        Console.Write(logLevel.ToString()[..4] + ": ");
        Console.ResetColor();
        Console.Write(header);
        Console.WriteLine(message);
        */
        AnsiConsole.MarkupLine(markup.Invoke(header + message.EscapeMarkup(), null));
    }

    private string Header()
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}] {Name}: ".EscapeMarkup();
    }
}