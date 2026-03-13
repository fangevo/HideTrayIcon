using TrayZen.Models;
using TrayZen.Native;

namespace TrayZen.Services;

public sealed class TrayIconService : ITrayIconService
{
    private readonly TrayIconInterop _interop = new();
    private readonly Lock _lock = new();
    private List<TrayIconInfo> _icons = [];
    private List<TrayIconInfo> _zenHiddenIcons = [];
    private CancellationTokenSource? _cts;

    public IReadOnlyList<TrayIconInfo> CurrentIcons
    {
        get { lock (_lock) return _icons.AsReadOnly(); }
    }

    public bool IsZenModeActive { get; private set; }

    public event Action<IReadOnlyList<TrayIconInfo>>? IconsChanged;
    public event Action<bool>? ZenModeChanged;

    public void StartPolling(int intervalMs)
    {
        StopPolling();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RefreshAsync();
                    await Task.Delay(intervalMs, token);
                }
                catch (OperationCanceledException) { break; }
                catch { /* continue polling */ }
            }
        }, token);
    }

    public void StopPolling()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public Task RefreshAsync()
    {
        var fresh = _interop.EnumerateAllIcons();

        lock (_lock)
        {
            foreach (var icon in fresh)
            {
                var prev = _icons.FirstOrDefault(i => i.UniqueKey == icon.UniqueKey);
                if (prev != null)
                {
                    icon.IsEssential = prev.IsEssential;
                    icon.GroupName = prev.GroupName;
                    icon.CustomIconPath = prev.CustomIconPath;
                }
            }
            _icons = fresh;
        }

        IconsChanged?.Invoke(fresh.AsReadOnly());
        return Task.CompletedTask;
    }

    public bool SetVisibility(TrayIconInfo icon, bool visible) =>
        visible ? _interop.ShowIcon(icon) : _interop.HideIcon(icon);

    public bool ReplaceIcon(TrayIconInfo icon, string icoPath) =>
        _interop.ReplaceIcon(icon, icoPath);

    public void ActivateZenMode(IEnumerable<string> essentialProcesses)
    {
        _zenHiddenIcons = _interop.HideAllExcept(essentialProcesses);
        IsZenModeActive = true;
        ZenModeChanged?.Invoke(true);
    }

    public void DeactivateZenMode()
    {
        _interop.RestoreAll(_zenHiddenIcons);
        _zenHiddenIcons.Clear();
        IsZenModeActive = false;
        ZenModeChanged?.Invoke(false);
    }

    public void ApplyProfile(TrayProfile profile)
    {
        lock (_lock)
        {
            foreach (var icon in _icons)
            {
                if (profile.IconStates.TryGetValue(icon.UniqueKey, out var state))
                {
                    SetVisibility(icon, state.IsVisible);
                    icon.GroupName = state.GroupName;
                    if (!string.IsNullOrEmpty(state.CustomIconPath))
                        ReplaceIcon(icon, state.CustomIconPath);
                }
            }
        }
    }

    public void SetGroupVisibility(string groupName, bool visible)
    {
        lock (_lock)
        {
            foreach (var icon in _icons.Where(i =>
                string.Equals(i.GroupName, groupName, StringComparison.OrdinalIgnoreCase)))
            {
                SetVisibility(icon, visible);
            }
        }
    }

    public string? GetForegroundProcessName() =>
        TrayIconInterop.GetForegroundProcessName();

    public void Dispose()
    {
        StopPolling();
        _interop.Dispose();
    }
}
