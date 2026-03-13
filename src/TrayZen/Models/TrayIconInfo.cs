using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayZen.Models;

public partial class TrayIconInfo : ObservableObject
{
    // ── Identity (populated during enumeration) ─────────────────────
    public IntPtr ToolbarHandle { get; set; }
    public int ButtonIndex { get; set; }
    public int CommandId { get; set; }
    public IntPtr OwnerWindowHandle { get; set; }
    public uint IconId { get; set; }
    public uint CallbackMessage { get; set; }

    private IntPtr _iconHandle;
    public IntPtr IconHandle
    {
        get => _iconHandle;
        set
        {
            _iconHandle = value;
            OnPropertyChanged(nameof(IconImageSource));
        }
    }

    // ── Observable display properties ───────────────────────────────

    [ObservableProperty] private string _tooltip = string.Empty;
    [ObservableProperty] private string _processName = "Unknown";
    [ObservableProperty] private string _processPath = string.Empty;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _isInOverflow;
    [ObservableProperty] private bool _isEssential;
    [ObservableProperty] private string? _customIconPath;
    [ObservableProperty] private string? _groupName;

    public string UniqueKey => $"{ProcessName}::{IconId}";

    public ImageSource? IconImageSource
    {
        get
        {
            if (_iconHandle == IntPtr.Zero) return null;
            try
            {
                var src = Imaging.CreateBitmapSourceFromHIcon(
                    _iconHandle, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                src.Freeze();
                return src;
            }
            catch { return null; }
        }
    }
}
