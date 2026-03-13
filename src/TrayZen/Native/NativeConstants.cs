namespace TrayZen.Native;

internal static class NativeConstants
{
    // Shell_NotifyIcon
    internal const uint NIM_MODIFY        = 0x00000001;
    internal const uint NIF_STATE         = 0x00000008;
    internal const uint NIS_HIDDEN        = 0x00000001;

    // Toolbar messages
    internal const uint TB_BUTTONCOUNT     = 0x0418;
    internal const uint TB_GETBUTTON       = 0x0417;
    internal const uint TB_HIDEBUTTON      = 0x0404;
    internal const uint TB_GETITEMRECT     = 0x041D;
    internal const uint TB_AUTOSIZE        = 0x0421;

    // SetWindowPos flags
    internal const uint SWP_NOMOVE         = 0x0002;
    internal const uint SWP_NOSIZE         = 0x0001;
    internal const uint SWP_NOZORDER       = 0x0004;
    internal const uint SWP_FRAMECHANGED   = 0x0020;
    internal const uint SWP_NOACTIVATE     = 0x0010;

    // Window messages
    internal const uint WM_SETTINGCHANGE   = 0x001A;
    internal const uint WM_MOUSEMOVE       = 0x0200;
    internal const uint WM_LBUTTONDOWN     = 0x0201;
    internal const uint WM_LBUTTONUP       = 0x0202;
    internal const uint WM_RBUTTONDOWN     = 0x0204;
    internal const uint WM_RBUTTONUP       = 0x0205;
    internal const int  WM_HOTKEY          = 0x0312;

    // Process access
    internal const uint PROCESS_VM_OPERATION        = 0x0008;
    internal const uint PROCESS_VM_READ             = 0x0010;
    internal const uint PROCESS_VM_WRITE            = 0x0020;
    internal const uint PROCESS_QUERY_INFORMATION   = 0x0400;
    internal const uint PROCESS_ALL_ACCESS          = PROCESS_VM_OPERATION | PROCESS_VM_READ
                                                    | PROCESS_VM_WRITE | PROCESS_QUERY_INFORMATION;

    // Memory allocation
    internal const uint MEM_COMMIT     = 0x1000;
    internal const uint MEM_RESERVE    = 0x2000;
    internal const uint MEM_RELEASE    = 0x8000;
    internal const uint PAGE_READWRITE = 0x04;

    // TBBUTTON state
    internal const byte TBSTATE_HIDDEN = 0x08;

    // Hotkey modifiers
    internal const int MOD_ALT      = 0x0001;
    internal const int MOD_CONTROL  = 0x0002;
    internal const int MOD_SHIFT    = 0x0004;
    internal const int MOD_WIN      = 0x0008;
    internal const int MOD_NOREPEAT = 0x4000;

    // LoadImage
    internal const uint IMAGE_ICON       = 1;
    internal const uint LR_LOADFROMFILE  = 0x0010;
    internal const uint LR_DEFAULTSIZE   = 0x0040;

    // Misc
    internal const int  MAX_PATH         = 260;
    internal const uint SMTO_ABORTIFHUNG = 0x0002;
}
