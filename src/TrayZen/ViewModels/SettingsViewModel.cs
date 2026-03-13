using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TrayZen.Models;
using TrayZen.Services;

namespace TrayZen.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly AutoHideService _autoHideService;

    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _useDarkMode;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private int _refreshInterval;
    [ObservableProperty] private string _hotkeyDisplay = string.Empty;
    [ObservableProperty] private bool _isRecordingHotkey;
    [ObservableProperty] private string _newAutoHideProcess = string.Empty;
    [ObservableProperty] private int _newAutoHideSeconds = 300;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<AutoHideRule> AutoHideRules { get; } = [];

    public event Action<bool>? ThemeChanged;
    public event Action<HotkeyBinding>? HotkeyChanged;

    private int _pendingModifiers;
    private int _pendingKey;

    public SettingsViewModel(SettingsService settingsService, AutoHideService autoHideService)
    {
        _settingsService = settingsService;
        _autoHideService = autoHideService;
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = _settingsService.Current;
        StartWithWindows = s.StartWithWindows;
        StartMinimized = s.StartMinimized;
        UseDarkMode = s.UseDarkMode;
        MinimizeToTray = s.MinimizeToTray;
        RefreshInterval = s.IconRefreshIntervalMs;
        HotkeyDisplay = s.ZenModeHotkey.DisplayText;

        AutoHideRules.Clear();
        foreach (var r in s.AutoHideRules) AutoHideRules.Add(r);
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        _settingsService.Current.StartWithWindows = value;
        SetStartupRegistry(value);
    }

    partial void OnStartMinimizedChanged(bool value) =>
        _settingsService.Current.StartMinimized = value;

    partial void OnUseDarkModeChanged(bool value)
    {
        _settingsService.Current.UseDarkMode = value;
        ThemeChanged?.Invoke(value);
    }

    partial void OnMinimizeToTrayChanged(bool value) =>
        _settingsService.Current.MinimizeToTray = value;

    partial void OnRefreshIntervalChanged(int value) =>
        _settingsService.Current.IconRefreshIntervalMs = value;

    // ── Hotkey recorder ─────────────────────────────────────────────

    [RelayCommand]
    private void StartRecordingHotkey() => IsRecordingHotkey = true;

    /// <summary>Called from code-behind on PreviewKeyDown when recording.</summary>
    public void RecordKey(Key key, ModifierKeys modifiers)
    {
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System)
            return;

        int mods = 0;
        if (modifiers.HasFlag(ModifierKeys.Control)) mods |= 0x0002;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mods |= 0x0001;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mods |= 0x0004;
        if (modifiers.HasFlag(ModifierKeys.Windows)) mods |= 0x0008;

        int vk = KeyInterop.VirtualKeyFromKey(key);

        _settingsService.Current.ZenModeHotkey = new HotkeyBinding { Modifiers = mods, Key = vk };
        HotkeyDisplay = _settingsService.Current.ZenModeHotkey.DisplayText;
        IsRecordingHotkey = false;

        HotkeyChanged?.Invoke(_settingsService.Current.ZenModeHotkey);
    }

    // ── Auto-hide rules ─────────────────────────────────────────────

    [RelayCommand]
    private void AddAutoHideRule()
    {
        if (string.IsNullOrWhiteSpace(NewAutoHideProcess)) return;

        var rule = new AutoHideRule
        {
            ProcessName = NewAutoHideProcess.Trim(),
            IdleThresholdSeconds = NewAutoHideSeconds,
            Enabled = true
        };
        AutoHideRules.Add(rule);
        _settingsService.Current.AutoHideRules.Add(rule);
        _autoHideService.UpdateRules([.. _settingsService.Current.AutoHideRules]);
        NewAutoHideProcess = string.Empty;
    }

    [RelayCommand]
    private void RemoveAutoHideRule(AutoHideRule? rule)
    {
        if (rule == null) return;
        AutoHideRules.Remove(rule);
        _settingsService.Current.AutoHideRules.Remove(rule);
        _autoHideService.UpdateRules([.. _settingsService.Current.AutoHideRules]);
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        await _settingsService.SaveAsync();
        StatusMessage = "Settings saved";
    }

    private static void SetStartupRegistry(bool enable)
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
        if (key == null) return;

        if (enable)
            key.SetValue("TrayZen", $"\"{Environment.ProcessPath}\" --minimized");
        else
            key.DeleteValue("TrayZen", false);
    }
}
