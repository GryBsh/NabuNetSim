namespace Nabu.Cli;

public static class ErrorMessage
{
    public static string NotFound(string type, string item)
        => $"{type} `{item}` not found";
}

public static class Markup
{
    public static string Error(object message, string? header = null)
    {
        header ??= "Fail";
        return $"[red]{header}:[/] {message}";
    }

    public static string Info(object message, string? header = null)
    {
        header ??= "Info";
        return $"[green]{header}:[/] {message}";
    }

    public static string Warning(object message, string? header = null)
    {
        header ??= "Warn";
        return $"[yellow]{header}:[/] {message}";
    }
}