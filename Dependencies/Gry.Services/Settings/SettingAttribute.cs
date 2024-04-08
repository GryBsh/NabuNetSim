namespace Gry.Settings;

public enum SettingValueType
{
    None,
    Source,
    Program,
    Package,
    LauncherSource,    LauncherProgram,
    Port
}

[AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute(string label) : Attribute
{

    public string Label { get; set; } = label;
    public string Section { get; set; } = "General";
    public string Description { get; set; } = string.Empty;
    public bool Advanced { get; set; } = false;
    public bool AllowEntry { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public SettingValueType Options { get; set; } = SettingValueType.None;
}
