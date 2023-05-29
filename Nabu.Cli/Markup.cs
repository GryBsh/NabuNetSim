namespace Nabu.Cli;

public static class ErrorMessage
{
    public static string NotFound(string type, string item)
        => $"{type} `{item}` not found";
}
public static class Markup
{

    public static string Error(object message, string header = "Error")
    {
        return $"[red]${header}:[/] ${message}";
    }
}