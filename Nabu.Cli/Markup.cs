namespace Nabu.Cli;
static class Markup
{
    public static string Error(object message, string header = "Error")
    {
        return $"[red]${header}:[/] ${message}";
    }
}