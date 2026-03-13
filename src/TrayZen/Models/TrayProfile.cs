namespace TrayZen.Models;

public class TrayProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Default";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, TrayIconState> IconStates { get; set; } = new();
    public List<string> EssentialProcesses { get; set; } = ["explorer", "SecurityHealthSystray"];
}

public class TrayIconState
{
    public bool IsVisible { get; set; } = true;
    public string? CustomIconPath { get; set; }
    public string? GroupName { get; set; }
}
