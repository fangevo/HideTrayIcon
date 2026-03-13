using System.Security.Principal;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrayZen.Services;

namespace TrayZen.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITrayIconService _trayService;
    private readonly SettingsService _settings;

    [ObservableProperty] private ObservableObject? _currentView;
    [ObservableProperty] private string _selectedNav = "Dashboard";
    [ObservableProperty] private bool _isZenModeActive;
    [ObservableProperty] private bool _isAdmin;
    [ObservableProperty] private string _statusText = "Ready";

    public DashboardViewModel Dashboard { get; }
    public ProfilesViewModel Profiles { get; }
    public SettingsViewModel Settings { get; }

    public event Action? ShowWindowRequested;
    public event Action? ExitRequested;

    public MainViewModel(
        ITrayIconService trayService,
        IProfileService profileService,
        SettingsService settings,
        DashboardViewModel dashboard,
        ProfilesViewModel profiles,
        SettingsViewModel settingsVm)
    {
        _trayService = trayService;
        _settings = settings;
        Dashboard = dashboard;
        Profiles = profiles;
        Settings = settingsVm;
        CurrentView = dashboard;

        _trayService.ZenModeChanged += active =>
        {
            IsZenModeActive = active;
            StatusText = active ? "Zen Mode active" : "Ready";
        };

        IsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
    }

    [RelayCommand]
    private void Navigate(string dest)
    {
        SelectedNav = dest;
        CurrentView = dest switch
        {
            "Profiles" => Profiles,
            "Settings" => Settings,
            _ => Dashboard
        };
    }

    [RelayCommand]
    private void ToggleZenMode()
    {
        if (_trayService.IsZenModeActive)
        {
            _trayService.DeactivateZenMode();
        }
        else
        {
            var essentials = Dashboard.Icons
                .Where(i => i.IsEssential)
                .Select(i => i.ProcessName)
                .Concat(["explorer", "TrayZen"])
                .Distinct(StringComparer.OrdinalIgnoreCase);
            _trayService.ActivateZenMode(essentials);
        }
    }

    [RelayCommand]
    private void ShowWindow() => ShowWindowRequested?.Invoke();

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke();
}
