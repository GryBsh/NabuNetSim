namespace Nabu.Configuration
{
    public interface IDatabaseSettings
    {
        string? FilePath { get; set; }
        int MaintenanceIntervalMinutes { get; set; }
    }
}