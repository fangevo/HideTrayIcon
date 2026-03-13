using TrayZen.Models;

namespace TrayZen.Services;

/// <summary>
/// Tracks which processes have recently been in the foreground.
/// If a process hasn't been in the foreground for longer than its
/// configured threshold, its tray icon is hidden. When it regains
/// foreground, the icon is restored.
/// </summary>
public sealed class AutoHideService : IDisposable
{
    private readonly ITrayIconService _trayService;
    private readonly Dictionary<string, DateTime> _lastForeground = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _currentlyAutoHidden = new(StringComparer.OrdinalIgnoreCase);
    private Timer? _timer;
    private List<AutoHideRule> _rules = [];

    public AutoHideService(ITrayIconService trayService)
    {
        _trayService = trayService;
    }

    public void UpdateRules(List<AutoHideRule> rules)
    {
        _rules = rules;
    }

    public void Start(int checkIntervalMs = 2000)
    {
        Stop();
        _timer = new Timer(OnTick, null, 0, checkIntervalMs);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void OnTick(object? state)
    {
        try
        {
            var fgProcess = _trayService.GetForegroundProcessName();
            if (!string.IsNullOrEmpty(fgProcess))
                _lastForeground[fgProcess] = DateTime.UtcNow;

            var enabledRules = _rules.Where(r => r.Enabled).ToList();
            if (enabledRules.Count == 0) return;

            var now = DateTime.UtcNow;
            var icons = _trayService.CurrentIcons;

            foreach (var rule in enabledRules)
            {
                var matchingIcons = icons.Where(i =>
                    string.Equals(i.ProcessName, rule.ProcessName, StringComparison.OrdinalIgnoreCase));

                bool hasRecentForeground = _lastForeground.TryGetValue(rule.ProcessName, out var last)
                    && (now - last).TotalSeconds <= rule.IdleThresholdSeconds;

                foreach (var icon in matchingIcons)
                {
                    if (hasRecentForeground)
                    {
                        if (_currentlyAutoHidden.Remove(rule.ProcessName) && !icon.IsVisible)
                            _trayService.SetVisibility(icon, true);
                    }
                    else if (icon.IsVisible)
                    {
                        _trayService.SetVisibility(icon, false);
                        _currentlyAutoHidden.Add(rule.ProcessName);
                    }
                }
            }
        }
        catch { /* swallow — background service must not crash */ }
    }

    public void Dispose()
    {
        Stop();
        _currentlyAutoHidden.Clear();
    }
}
