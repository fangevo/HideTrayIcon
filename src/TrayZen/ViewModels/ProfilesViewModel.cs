using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TrayZen.Models;
using TrayZen.Services;

namespace TrayZen.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    private readonly IProfileService _profileService;
    private readonly ITrayIconService _trayService;

    public ObservableCollection<TrayProfile> Profiles { get; } = [];

    [ObservableProperty] private TrayProfile? _selectedProfile;
    [ObservableProperty] private string _newProfileName = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ProfilesViewModel(IProfileService profileService, ITrayIconService trayService)
    {
        _profileService = profileService;
        _trayService = trayService;
        _profileService.ActiveProfileChanged += p => SelectedProfile = p;
    }

    [RelayCommand]
    private async Task LoadProfiles()
    {
        await _profileService.LoadAsync();
        Profiles.Clear();
        foreach (var p in _profileService.Profiles) Profiles.Add(p);
        SelectedProfile = _profileService.ActiveProfile;
    }

    [RelayCommand]
    private async Task CreateProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName)) return;
        var p = _profileService.CaptureCurrentState(NewProfileName, _trayService.CurrentIcons);
        Profiles.Add(p);
        NewProfileName = string.Empty;
        await _profileService.SaveAsync();
        StatusMessage = $"Profile \"{p.Name}\" created";
    }

    [RelayCommand]
    private async Task DeleteProfile(TrayProfile? profile)
    {
        if (profile == null) return;
        _profileService.DeleteProfile(profile.Id);
        Profiles.Remove(profile);
        await _profileService.SaveAsync();
        StatusMessage = $"Profile \"{profile.Name}\" deleted";
    }

    [RelayCommand]
    private void ActivateProfile(TrayProfile? profile)
    {
        if (profile == null) return;
        _profileService.SetActiveProfile(profile.Id);
        _trayService.ApplyProfile(profile);
        StatusMessage = $"Profile \"{profile.Name}\" activated";
    }

    [RelayCommand]
    private async Task ExportProfile(TrayProfile? profile)
    {
        if (profile == null) return;
        var dlg = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            FileName = $"{profile.Name}.json",
            Title = "Export Profile"
        };
        if (dlg.ShowDialog() == true)
        {
            await _profileService.ExportAsync(profile.Id, dlg.FileName);
            StatusMessage = $"Exported to {dlg.FileName}";
        }
    }

    [RelayCommand]
    private async Task ImportProfile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            Title = "Import Profile"
        };
        if (dlg.ShowDialog() != true) return;

        var p = await _profileService.ImportAsync(dlg.FileName);
        if (p != null)
        {
            Profiles.Add(p);
            StatusMessage = $"Imported \"{p.Name}\"";
        }
        else
        {
            StatusMessage = "Import failed — invalid file";
        }
    }
}
