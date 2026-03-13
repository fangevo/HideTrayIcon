using System.Windows;
using System.Windows.Interop;
using TrayZen.Native;
using static TrayZen.Native.NativeConstants;

namespace TrayZen.Services;

public sealed class HotkeyService : IDisposable
{
    private IntPtr _hwnd;
    private HwndSource? _source;
    private readonly Dictionary<int, Action> _hotkeys = [];
    private int _nextId = 9000;

    public void Initialize(Window window)
    {
        _hwnd = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);
    }

    public int Register(int modifiers, int key, Action callback)
    {
        int id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_hwnd, id, modifiers | MOD_NOREPEAT, key))
            return -1;
        _hotkeys[id] = callback;
        return id;
    }

    public void Unregister(int id)
    {
        NativeMethods.UnregisterHotKey(_hwnd, id);
        _hotkeys.Remove(id);
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeys.Keys.ToList())
            Unregister(id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _hotkeys.TryGetValue(wParam.ToInt32(), out var cb))
        {
            cb();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source?.RemoveHook(WndProc);
    }
}
