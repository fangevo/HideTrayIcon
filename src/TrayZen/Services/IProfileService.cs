using TrayZen.Models;

namespace TrayZen.Services;

public interface IProfileService
{
    IReadOnlyList<TrayProfile> Profiles { get; }
    TrayProfile? ActiveProfile { get; }

    Task LoadAsync();
    Task SaveAsync();
    TrayProfile CreateProfile(string name);
    bool DeleteProfile(string id);
    void SetActiveProfile(string id);
    TrayProfile CaptureCurrentState(string name, IReadOnlyList<TrayIconInfo> icons);
    Task<string> ExportAsync(string id, string filePath);
    Task<TrayProfile?> ImportAsync(string filePath);

    event Action<TrayProfile?>? ActiveProfileChanged;
}
