using TrayZen.Models;

namespace TrayZen.Services;

public interface ITrayIconService : IDisposable
{
    IReadOnlyList<TrayIconInfo> CurrentIcons { get; }
    bool IsZenModeActive { get; }

    Task RefreshAsync();
    void StartPolling(int intervalMs);
    void StopPolling();
    bool SetVisibility(TrayIconInfo icon, bool visible);
    bool ReplaceIcon(TrayIconInfo icon, string icoPath);
    void ActivateZenMode(IEnumerable<string> essentialProcesses);
    void DeactivateZenMode();
    void ApplyProfile(TrayProfile profile);
    void SetGroupVisibility(string groupName, bool visible);
    string? GetForegroundProcessName();

    event Action<IReadOnlyList<TrayIconInfo>>? IconsChanged;
    event Action<bool>? ZenModeChanged;
}
