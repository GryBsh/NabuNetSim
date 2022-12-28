namespace Nabu.Network;

public enum DefinitionType
{
    Unknown = 0,
    NabuRetroNet,
    Folder
}

public record ImageSourceDefinition
{
    
    public DefinitionType Type { get; set; } = DefinitionType.NabuRetroNet;
    public string? ListUrl { get; set; }
    public string? NabuRoot { get; set; }
    public string? PakRoot { get; set; }
}
