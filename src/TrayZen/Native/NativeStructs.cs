using System.Runtime.InteropServices;

namespace TrayZen.Native;

internal static class NativeStructs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TBBUTTON64
    {
        public int iBitmap;
        public int idCommand;
        public byte fsState;
        public byte fsStyle;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] bReserved;
        public long dwData;
        public long iString;
    }

    /// <summary>
    /// Explorer's internal representation of a notification icon.
    /// Embedded in the toolbar button's dwData pointer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TRAYDATA
    {
        public IntPtr hwnd;
        public uint uID;
        public uint uCallbackMessage;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] Reserved;
        public IntPtr hIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }
}
