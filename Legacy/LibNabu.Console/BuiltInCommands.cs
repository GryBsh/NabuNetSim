internal class BuiltInCommands
{
    public const string Exit = "exit";
    public const string Help = "help";
    public const string Clear = "clear";


    public static string[] List { get; } = new[]
    {
        Exit,
        Help,
        Clear
    };
}