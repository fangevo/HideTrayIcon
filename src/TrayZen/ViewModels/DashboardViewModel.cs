using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TrayZen.Models;
using TrayZen.Services;

namespace TrayZen.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ITrayIconService _trayService;

    public ObservableCollection<TrayIconInfo> Icons { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _visibleCount;
    [ObservableProperty] private int _hiddenCount;

    public DashboardViewModel(ITrayIconService trayService)
    {
        _trayService = trayService;
        _trayService.IconsChanged += OnIconsChanged;
    }

    private void OnIconsChanged(IReadOnlyList<TrayIconInfo> icons)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null) return;

        dispatcher.BeginInvoke(() =>
        {
            try
            {
                var filtered = icons.Where(MatchesSearch).ToList();
                var newKeys = new HashSet<string>(filtered.Select(i => i.UniqueKey));

                // Remove items that no longer exist
                for (int i = Icons.Count - 1; i >= 0; i--)
                {
                    if (!newKeys.Contains(Icons[i].UniqueKey))
                        Icons.RemoveAt(i);
                }

                // Update existing in-place or add new
                var existingByKey = Icons.ToDictionary(i => i.UniqueKey);
                foreach (var icon in filtered)
                {
                    if (existingByKey.TryGetValue(icon.UniqueKey, out var existing))
                    {
                        // Update mutable properties in-place (no rebind, no animation retrigger)
                        existing.IsVisible = icon.IsVisible;
                        existing.Tooltip = icon.Tooltip;
                        existing.IsInOverflow = icon.IsInOverflow;
                        existing.IconHandle = icon.IconHandle;
                        existing.ToolbarHandle = icon.ToolbarHandle;
                        existing.ButtonIndex = icon.ButtonIndex;
                        existing.CommandId = icon.CommandId;
                        existing.OwnerWindowHandle = icon.OwnerWindowHandle;
                    }
                    else
                    {
                        Icons.Add(icon);
                    }
                }

                TotalCount = icons.Count;
                VisibleCount = icons.Count(i => i.IsVisible);
                HiddenCount = icons.Count(i => !i.IsVisible);
            }
            catch { /* UI update failed — will retry on next poll */ }
        });
    }

    private bool MatchesSearch(TrayIconInfo icon) =>
        string.IsNullOrWhiteSpace(SearchText) ||
        icon.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
        icon.Tooltip.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

    partial void OnSearchTextChanged(string value)
    {
        // Search change needs a full rebuild since the filter changed
        var icons = _trayService.CurrentIcons;
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null) return;

        dispatcher.BeginInvoke(() =>
        {
            try
            {
                Icons.Clear();
                foreach (var i in icons)
                    if (MatchesSearch(i)) Icons.Add(i);
            }
            catch { }
        });
    }

    [RelayCommand]
    private void ToggleVisibility(TrayIconInfo? icon)
    {
        if (icon == null) return;
        _trayService.SetVisibility(icon, !icon.IsVisible);
    }

    [RelayCommand]
    private void ToggleEssential(TrayIconInfo? icon)
    {
        if (icon != null) icon.IsEssential = !icon.IsEssential;
    }

    [RelayCommand]
    private void ReplaceIcon(TrayIconInfo? icon)
    {
        if (icon == null) return;
        var dlg = new OpenFileDialog
        {
            Filter = "Icon Files (*.ico)|*.ico",
            Title = "Select replacement icon"
        };
        if (dlg.ShowDialog() == true)
            _trayService.ReplaceIcon(icon, dlg.FileName);
    }

    [RelayCommand]
    private void ClearCustomIcon(TrayIconInfo? icon)
    {
        if (icon != null) icon.CustomIconPath = null;
    }

    [RelayCommand]
    private void HideGroup(string? group)
    {
        if (!string.IsNullOrEmpty(group))
            _trayService.SetGroupVisibility(group, false);
    }

    [RelayCommand]
    private void ShowGroup(string? group)
    {
        if (!string.IsNullOrEmpty(group))
            _trayService.SetGroupVisibility(group, true);
    }

    [RelayCommand]
    private async Task RefreshIcons() => await _trayService.RefreshAsync();
}
