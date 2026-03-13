using System.IO;
using System.Text.Json;
using TrayZen.Models;

namespace TrayZen.Services;

public sealed class ProfileService : IProfileService
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TrayZen", "profiles");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly List<TrayProfile> _profiles = [];

    public IReadOnlyList<TrayProfile> Profiles => _profiles.AsReadOnly();
    public TrayProfile? ActiveProfile { get; private set; }
    public event Action<TrayProfile?>? ActiveProfileChanged;

    public async Task LoadAsync()
    {
        Directory.CreateDirectory(Dir);
        _profiles.Clear();

        foreach (var file in Directory.EnumerateFiles(Dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var p = JsonSerializer.Deserialize<TrayProfile>(json, Json);
                if (p != null) _profiles.Add(p);
            }
            catch { /* skip corrupt files */ }
        }

        if (_profiles.Count == 0)
            _profiles.Add(new TrayProfile { Name = "Default" });
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(Dir);
        foreach (var p in _profiles)
        {
            p.ModifiedAt = DateTime.UtcNow;
            var path = Path.Combine(Dir, $"{p.Id}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(p, Json));
        }
    }

    public TrayProfile CreateProfile(string name)
    {
        var p = new TrayProfile { Name = name };
        _profiles.Add(p);
        return p;
    }

    public bool DeleteProfile(string id)
    {
        var p = _profiles.FirstOrDefault(x => x.Id == id);
        if (p == null) return false;

        _profiles.Remove(p);
        var path = Path.Combine(Dir, $"{id}.json");
        if (File.Exists(path)) File.Delete(path);

        if (ActiveProfile?.Id == id)
        {
            ActiveProfile = _profiles.FirstOrDefault();
            ActiveProfileChanged?.Invoke(ActiveProfile);
        }
        return true;
    }

    public void SetActiveProfile(string id)
    {
        ActiveProfile = _profiles.FirstOrDefault(x => x.Id == id);
        ActiveProfileChanged?.Invoke(ActiveProfile);
    }

    public TrayProfile CaptureCurrentState(string name, IReadOnlyList<TrayIconInfo> icons)
    {
        var p = new TrayProfile { Name = name };
        foreach (var icon in icons)
        {
            p.IconStates[icon.UniqueKey] = new TrayIconState
            {
                IsVisible = icon.IsVisible,
                CustomIconPath = icon.CustomIconPath,
                GroupName = icon.GroupName,
            };
        }
        _profiles.Add(p);
        return p;
    }

    public async Task<string> ExportAsync(string id, string filePath)
    {
        var p = _profiles.FirstOrDefault(x => x.Id == id);
        if (p == null) return string.Empty;
        var json = JsonSerializer.Serialize(p, Json);
        await File.WriteAllTextAsync(filePath, json);
        return filePath;
    }

    public async Task<TrayProfile?> ImportAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var p = JsonSerializer.Deserialize<TrayProfile>(json, Json);
            if (p == null) return null;
            p.Id = Guid.NewGuid().ToString();
            _profiles.Add(p);
            await SaveAsync();
            return p;
        }
        catch { return null; }
    }
}
