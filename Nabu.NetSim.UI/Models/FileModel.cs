namespace Nabu.NetSim.UI.Models;

public record FileModel
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsSymLink { get; set; } = false;
}