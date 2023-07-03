namespace Nabu.Configuration
{
    public interface ILogSettings
    {
        int CleanupIntervalMinutes { get; set; }
        int MaxAgeDays { get; set; }
    }
}