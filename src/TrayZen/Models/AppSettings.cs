namespace TrayZen.Models;

public class AppSettings
{
    public string ActiveProfileId { get; set; } = string.Empty;
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; } = true;
    public bool UseDarkMode { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public int IconRefreshIntervalMs { get; set; } = 3000;

    public HotkeyBinding ZenModeHotkey { get; set; } = new()
    {
        Modifiers = 0x0002 | 0x0001, // Ctrl + Alt
        Key = 0x5A                    // Z
    };

    public List<AutoHideRule> AutoHideRules { get; set; } = [];
}

public class HotkeyBinding
{
    public int Modifiers { get; set; }
    public int Key { get; set; }

    public string DisplayText
    {
        get
        {
            var parts = new List<string>();
            if ((Modifiers & 0x0002) != 0) parts.Add("Ctrl");
            if ((Modifiers & 0x0001) != 0) parts.Add("Alt");
            if ((Modifiers & 0x0004) != 0) parts.Add("Shift");
            if ((Modifiers & 0x0008) != 0) parts.Add("Win");
            if (Key > 0) parts.Add(((System.Windows.Input.Key)System.Windows.Input.KeyInterop.KeyFromVirtualKey(Key)).ToString());
            return string.Join(" + ", parts);
        }
    }
}

public class AutoHideRule
{
    public string ProcessName { get; set; } = string.Empty;
    public int IdleThresholdSeconds { get; set; } = 300;
    public bool Enabled { get; set; } = true;
}
