using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TrayZen.Models;
using static TrayZen.Native.NativeConstants;
using static TrayZen.Native.NativeMethods;
using static TrayZen.Native.NativeStructs;

namespace TrayZen.Native;

/// <summary>
/// Low-level interop with the Windows Explorer notification area toolbar.
/// Window hierarchy:
///   Shell_TrayWnd → TrayNotifyWnd → SysPager → ToolbarWindow32 (visible)
///   NotifyIconOverflowWindow → ToolbarWindow32 (overflow)
/// </summary>
internal sealed class TrayIconInterop : IDisposable
{
    private readonly List<IntPtr> _ownedIcons = [];

    private static IntPtr FindTrayToolbar()
    {
        var shell = FindWindowW("Shell_TrayWnd", null);
        if (shell == IntPtr.Zero) return IntPtr.Zero;
        var notify = FindWindowExW(shell, IntPtr.Zero, "TrayNotifyWnd", null);
        if (notify == IntPtr.Zero) return IntPtr.Zero;
        var pager = FindWindowExW(notify, IntPtr.Zero, "SysPager", null);
        if (pager == IntPtr.Zero) return IntPtr.Zero;
        return FindWindowExW(pager, IntPtr.Zero, "ToolbarWindow32", null);
    }

    private static IntPtr FindOverflowToolbar()
    {
        var overflow = FindWindowW("NotifyIconOverflowWindow", null);
        return overflow == IntPtr.Zero
            ? IntPtr.Zero
            : FindWindowExW(overflow, IntPtr.Zero, "ToolbarWindow32", null);
    }

    public List<TrayIconInfo> EnumerateAllIcons()
    {
        var result = new List<TrayIconInfo>();
        try
        {
            var visible = FindTrayToolbar();
            if (visible != IntPtr.Zero)
                result.AddRange(EnumerateToolbar(visible, false));

            var overflow = FindOverflowToolbar();
            if (overflow != IntPtr.Zero)
                result.AddRange(EnumerateToolbar(overflow, true));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TrayIconInterop] Enumerate failed: {ex.Message}");
        }
        return result;
    }

    private static List<TrayIconInfo> EnumerateToolbar(IntPtr toolbar, bool isOverflow)
    {
        var icons = new List<TrayIconInfo>();

        GetWindowThreadProcessId(toolbar, out var pid);
        var hProc = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
        if (hProc == IntPtr.Zero) return icons;

        try
        {
            int count = (int)SendMessageW(toolbar, TB_BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
            if (count <= 0) return icons;

            int tbSize = Marshal.SizeOf<TBBUTTON64>();
            uint allocSize = (uint)(tbSize + 512);
            var remote = VirtualAllocEx(hProc, IntPtr.Zero, allocSize,
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remote == IntPtr.Zero) return icons;

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var info = ReadButton(toolbar, hProc, remote, i, tbSize, isOverflow);
                    if (info != null)
                        icons.Add(info);
                }
            }
            finally
            {
                VirtualFreeEx(hProc, remote, 0, MEM_RELEASE);
            }
        }
        finally
        {
            CloseHandle(hProc);
        }

        return icons;
    }

    private static TrayIconInfo? ReadButton(
        IntPtr toolbar, IntPtr hProc, IntPtr remote,
        int index, int tbSize, bool isOverflow)
    {
        SendMessageW(toolbar, TB_GETBUTTON, (IntPtr)index, remote);

        var local = Marshal.AllocHGlobal(tbSize);
        try
        {
            if (!ReadProcessMemory(hProc, remote, local, (uint)tbSize, out _))
                return null;

            var btn = Marshal.PtrToStructure<TBBUTTON64>(local);
            if (btn.dwData == 0) return null;

            int tdSize = Marshal.SizeOf<TRAYDATA>();
            var tdLocal = Marshal.AllocHGlobal(tdSize);
            try
            {
                if (!ReadProcessMemory(hProc, (IntPtr)btn.dwData, tdLocal, (uint)tdSize, out _))
                    return null;

                var td = Marshal.PtrToStructure<TRAYDATA>(tdLocal);
                string tooltip = ReadTooltip(hProc, btn);
                string procName = GetProcessName(td.hwnd);
                string procPath = GetProcessPath(td.hwnd);

                return new TrayIconInfo
                {
                    ToolbarHandle = toolbar,
                    ButtonIndex = index,
                    CommandId = btn.idCommand,
                    OwnerWindowHandle = td.hwnd,
                    IconHandle = td.hIcon,
                    IconId = td.uID,
                    CallbackMessage = td.uCallbackMessage,
                    Tooltip = tooltip,
                    ProcessName = procName,
                    ProcessPath = procPath,
                    IsVisible = (btn.fsState & TBSTATE_HIDDEN) == 0,
                    IsInOverflow = isOverflow,
                };
            }
            finally { Marshal.FreeHGlobal(tdLocal); }
        }
        finally { Marshal.FreeHGlobal(local); }
    }

    private static string ReadTooltip(IntPtr hProc, TBBUTTON64 btn)
    {
        if (btn.iString <= 0) return string.Empty;
        var buf = Marshal.AllocHGlobal(512);
        try
        {
            return ReadProcessMemory(hProc, (IntPtr)btn.iString, buf, 512, out _)
                ? Marshal.PtrToStringUni(buf) ?? string.Empty
                : string.Empty;
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    private static string GetProcessName(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return "Unknown";
        try
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch { return "Unknown"; }
    }

    private static string GetProcessPath(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return string.Empty;
        try
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            var h = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return string.Empty;
            try
            {
                var buf = new char[MAX_PATH];
                uint sz = (uint)buf.Length;
                return QueryFullProcessImageNameW(h, 0, buf, ref sz) != 0
                    ? new string(buf, 0, (int)sz) : string.Empty;
            }
            finally { CloseHandle(h); }
        }
        catch { return string.Empty; }
    }

    // ── Visibility ──────────────────────────────────────────────────

    public bool HideIcon(TrayIconInfo icon)
    {
        if (icon.ToolbarHandle == IntPtr.Zero) return false;
        try
        {
            // Prefer shell-supported state hiding to avoid leaving a blank slot.
            bool hidden = TrySetShellHiddenState(icon, true);
            if (!hidden)
                SendMessageW(icon.ToolbarHandle, TB_HIDEBUTTON, (IntPtr)icon.CommandId, (IntPtr)1);
            icon.IsVisible = false;
            RefreshTray();
            return true;
        }
        catch { return false; }
    }

    public bool ShowIcon(TrayIconInfo icon)
    {
        if (icon.ToolbarHandle == IntPtr.Zero) return false;
        try
        {
            bool shown = TrySetShellHiddenState(icon, false);
            if (!shown)
                SendMessageW(icon.ToolbarHandle, TB_HIDEBUTTON, (IntPtr)icon.CommandId, IntPtr.Zero);
            icon.IsVisible = true;
            RefreshTray();
            return true;
        }
        catch { return false; }
    }

    public List<TrayIconInfo> HideAllExcept(IEnumerable<string> essentialNames)
    {
        var set = new HashSet<string>(essentialNames, StringComparer.OrdinalIgnoreCase);
        var hidden = new List<TrayIconInfo>();
        foreach (var icon in EnumerateAllIcons())
        {
            if (icon.IsVisible && !set.Contains(icon.ProcessName))
            {
                if (HideIcon(icon))
                    hidden.Add(icon);
            }
        }
        return hidden;
    }

    public void RestoreAll(IEnumerable<TrayIconInfo> icons)
    {
        foreach (var icon in icons)
            ShowIcon(icon);
    }

    // ── Icon replacement ────────────────────────────────────────────

    /// <summary>
    /// Replaces a tray icon's image by overwriting the HICON pointer in
    /// Explorer's TRAYDATA via WriteProcessMemory. Uses LoadImageW to
    /// load the .ico file (no System.Drawing dependency).
    /// </summary>
    public bool ReplaceIcon(TrayIconInfo icon, string icoPath)
    {
        if (!File.Exists(icoPath) || icon.ToolbarHandle == IntPtr.Zero)
            return false;

        var hNew = LoadImageW(IntPtr.Zero, icoPath, IMAGE_ICON, 16, 16,
            LR_LOADFROMFILE | LR_DEFAULTSIZE);
        if (hNew == IntPtr.Zero) return false;

        _ownedIcons.Add(hNew);

        GetWindowThreadProcessId(icon.ToolbarHandle, out var pid);
        var hProc = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
        if (hProc == IntPtr.Zero) return false;

        try
        {
            int tbSize = Marshal.SizeOf<TBBUTTON64>();
            var remote = VirtualAllocEx(hProc, IntPtr.Zero, (uint)(tbSize + 512),
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remote == IntPtr.Zero) return false;

            try
            {
                SendMessageW(icon.ToolbarHandle, TB_GETBUTTON, (IntPtr)icon.ButtonIndex, remote);
                var local = Marshal.AllocHGlobal(tbSize);
                try
                {
                    if (!ReadProcessMemory(hProc, remote, local, (uint)tbSize, out _))
                        return false;
                    var btn = Marshal.PtrToStructure<TBBUTTON64>(local);
                    if (btn.dwData == 0) return false;

                    int tdSize = Marshal.SizeOf<TRAYDATA>();
                    var tdLocal = Marshal.AllocHGlobal(tdSize);
                    try
                    {
                        if (!ReadProcessMemory(hProc, (IntPtr)btn.dwData, tdLocal, (uint)tdSize, out _))
                            return false;
                        var td = Marshal.PtrToStructure<TRAYDATA>(tdLocal);
                        td.hIcon = hNew;
                        Marshal.StructureToPtr(td, tdLocal, false);
                        if (!WriteProcessMemory(hProc, (IntPtr)btn.dwData, tdLocal, (uint)tdSize, out _))
                            return false;

                        icon.IconHandle = hNew;
                        icon.CustomIconPath = icoPath;
                        RefreshTray();
                        return true;
                    }
                    finally { Marshal.FreeHGlobal(tdLocal); }
                }
                finally { Marshal.FreeHGlobal(local); }
            }
            finally { VirtualFreeEx(hProc, remote, 0, MEM_RELEASE); }
        }
        finally { CloseHandle(hProc); }
    }

    // ── Foreground tracking (for auto-hide) ─────────────────────────

    public static string? GetForegroundProcessName()
    {
        try
        {
            var fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return null;
            GetWindowThreadProcessId(fg, out var pid);
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch { return null; }
    }

    public static uint GetIdleTimeMs()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        return GetLastInputInfo(ref info)
            ? GetTickCount() - info.dwTime
            : 0;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static bool TrySetShellHiddenState(TrayIconInfo icon, bool isHidden)
    {
        if (icon.OwnerWindowHandle == IntPtr.Zero)
            return false;

        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = icon.OwnerWindowHandle,
            uID = icon.IconId,
            uFlags = NIF_STATE,
            dwStateMask = NIS_HIDDEN,
            dwState = isHidden ? NIS_HIDDEN : 0,
            szTip = string.Empty,
            szInfo = string.Empty,
            szInfoTitle = string.Empty,
        };

        return Shell_NotifyIconW(NIM_MODIFY, ref data);
    }

    /// <summary>
    /// Forces the notification area to reclaim blank space after hiding icons.
    /// Uses TB_AUTOSIZE + SWP_FRAMECHANGED to trigger a full layout pass
    /// up the entire Shell_TrayWnd → TrayNotifyWnd → SysPager → Toolbar chain.
    /// </summary>
    private static void RefreshTray()
    {
        var shell = FindWindowW("Shell_TrayWnd", null);
        if (shell == IntPtr.Zero) return;

        var trayNotify = FindWindowExW(shell, IntPtr.Zero, "TrayNotifyWnd", null);
        var sysPager = trayNotify != IntPtr.Zero
            ? FindWindowExW(trayNotify, IntPtr.Zero, "SysPager", null) : IntPtr.Zero;
        var toolbar = sysPager != IntPtr.Zero
            ? FindWindowExW(sysPager, IntPtr.Zero, "ToolbarWindow32", null) : IntPtr.Zero;

        // 1) Tell the toolbar to auto-size based on visible buttons
        if (toolbar != IntPtr.Zero)
        {
            SendMessageW(toolbar, TB_AUTOSIZE, IntPtr.Zero, IntPtr.Zero);
            InvalidateRect(toolbar, IntPtr.Zero, true);
            UpdateWindow(toolbar);
        }

        // 2) Force SWP_FRAMECHANGED up the parent chain to trigger a full relayout
        const uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER
                         | SWP_FRAMECHANGED | SWP_NOACTIVATE;

        if (sysPager != IntPtr.Zero)
            SetWindowPos(sysPager, IntPtr.Zero, 0, 0, 0, 0, flags);
        if (trayNotify != IntPtr.Zero)
            SetWindowPos(trayNotify, IntPtr.Zero, 0, 0, 0, 0, flags);

        // 3) Poke the taskbar itself to recalculate its layout
        if (GetClientRect(shell, out var rc))
        {
            int w = rc.Right - rc.Left;
            int h = rc.Bottom - rc.Top;
            IntPtr lp = (IntPtr)((h << 16) | (w & 0xFFFF));
            SendMessageW(shell, 0x0005 /*WM_SIZE*/, IntPtr.Zero, lp);
        }
    }

    public void Dispose()
    {
        foreach (var h in _ownedIcons)
            DestroyIcon(h);
        _ownedIcons.Clear();
    }
}
