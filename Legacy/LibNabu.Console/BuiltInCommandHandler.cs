using Spectre.Console;

namespace Nabu.Cli;

public class BuiltInCommandHandler : Dictionary<string, Func<string, (bool, string)>>
{
    public CancellationTokenSource Cancellation { get; }
    public BuiltInCommandHandler(CancellationTokenSource cancellation)
    {
        Cancellation = cancellation;
        this[BuiltInCommands.Exit] = Exit;
        this[BuiltInCommands.Help] = Help;
        this[BuiltInCommands.Clear] = End;
    }

    protected (bool, string) Help(string cmd)
    {
        return (true, "-h");
    }

    protected (bool, string) Exit(string cmd)
    {
        Cancellation.Cancel();
        return (false, string.Empty);
    }

    protected (bool, string) End(string cmd)
    {
        AnsiConsole.Clear();
        return (false, string.Empty);
    }

}