using System.IO;
using System.Text.Json;
using TrayZen.Models;

namespace TrayZen.Services;

public sealed class SettingsService
{
    private static readonly string Path_ = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TrayZen", "settings.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings Current { get; private set; } = new();

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(Path_))
            {
                var json = await File.ReadAllTextAsync(Path_);
                Current = JsonSerializer.Deserialize<AppSettings>(json, Json) ?? new();
            }
        }
        catch { Current = new AppSettings(); }
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path_)!);
        await File.WriteAllTextAsync(Path_, JsonSerializer.Serialize(Current, Json));
    }
}
