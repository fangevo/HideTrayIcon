using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using TrayZen.Services;
using TrayZen.ViewModels;

namespace TrayZen;

public partial class App : Application
{
    private ServiceProvider? _sp;
    private HotkeyService? _hotkeyService;
    private AutoHideService? _autoHideService;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTask;

        try
        {
            var sc = new ServiceCollection();
            RegisterServices(sc);
            _sp = sc.BuildServiceProvider();

            var settings = _sp.GetRequiredService<SettingsService>();
            await settings.LoadAsync();

            ApplyTheme(settings.Current.UseDarkMode ? "DarkTheme" : "LightTheme");

            var mainVm = _sp.GetRequiredService<MainViewModel>();

            _mainWindow = new MainWindow { DataContext = mainVm };
            MainWindow = _mainWindow;

            mainVm.ShowWindowRequested += () => ShowMainWindow();
            mainVm.ExitRequested += () => Shutdown();
            _mainWindow.Closing += (_, args) =>
            {
                if (settings.Current.MinimizeToTray)
                {
                    args.Cancel = true;
                    _mainWindow.Hide();
                }
                else
                {
                    Shutdown();
                }
            };

            var settingsVm = _sp.GetRequiredService<SettingsViewModel>();
            settingsVm.ThemeChanged += isDark =>
                ApplyTheme(isDark ? "DarkTheme" : "LightTheme");

            _mainWindow.Show();

            // Hotkeys — must be after Show() so HWND exists
            _hotkeyService = _sp.GetRequiredService<HotkeyService>();
            _hotkeyService.Initialize(_mainWindow);
            var hk = settings.Current.ZenModeHotkey;
            _hotkeyService.Register(hk.Modifiers, hk.Key,
                () => mainVm.ToggleZenModeCommand.Execute(null));

            settingsVm.HotkeyChanged += newHk =>
            {
                _hotkeyService.UnregisterAll();
                _hotkeyService.Register(newHk.Modifiers, newHk.Key,
                    () => mainVm.ToggleZenModeCommand.Execute(null));
            };

            // Tray icon — fail-safe, app works without it
            try
            {
                _trayIcon = CreateTrayIcon(mainVm);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Tray icon creation failed: {ex.Message}");
            }

            // Start polling
            var trayService = (TrayIconService)_sp.GetRequiredService<ITrayIconService>();
            trayService.StartPolling(settings.Current.IconRefreshIntervalMs);

            // Auto-hide
            _autoHideService = _sp.GetRequiredService<AutoHideService>();
            _autoHideService.UpdateRules(settings.Current.AutoHideRules);
            _autoHideService.Start();

            // Load profiles
            var profileService = _sp.GetRequiredService<IProfileService>();
            await profileService.LoadAsync();

            // Initial refresh
            await trayService.RefreshAsync();

            if (settings.Current.StartMinimized && e.Args.Contains("--minimized"))
                _mainWindow.Hide();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error:\n\n{ex}", "TrayZen",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private TaskbarIcon CreateTrayIcon(MainViewModel vm)
    {
        var icon = new TaskbarIcon
        {
            ToolTipText = "TrayZen",
            IconSource = LoadTrayIconSource(),
        };

        icon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        var menu = new System.Windows.Controls.ContextMenu();
        menu.Items.Add(new System.Windows.Controls.MenuItem
            { Header = "Show TrayZen", Command = vm.ShowWindowCommand });
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(new System.Windows.Controls.MenuItem
            { Header = "Toggle Zen Mode", Command = vm.ToggleZenModeCommand });
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(new System.Windows.Controls.MenuItem
            { Header = "Exit", Command = vm.ExitCommand });
        icon.ContextMenu = menu;

        icon.ForceCreate();
        return icon;
    }

    private static ImageSource LoadTrayIconSource()
    {
        try
        {
            var resourceUri = new Uri("pack://application:,,,/Assets/trayzen.ico", UriKind.Absolute);
            return new BitmapImage(resourceUri);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Failed to load trayzen.ico: {ex.Message}");
        }

        // Guaranteed fallback so the tray icon is always visible.
        return Imaging.CreateBitmapSourceFromHIcon(
            SystemIcons.Application.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(16, 16));
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.WindowState = WindowState.Normal;
    }

    private static void RegisterServices(IServiceCollection sc)
    {
        sc.AddSingleton<ITrayIconService, TrayIconService>();
        sc.AddSingleton<IProfileService, ProfileService>();
        sc.AddSingleton<SettingsService>();
        sc.AddSingleton<HotkeyService>();
        sc.AddSingleton<AutoHideService>();
        sc.AddSingleton<MainViewModel>();
        sc.AddSingleton<DashboardViewModel>();
        sc.AddSingleton<ProfilesViewModel>();
        sc.AddSingleton<SettingsViewModel>();
    }

    private void ApplyTheme(string name)
    {
        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri($"Themes/{name}.xaml", UriKind.Relative) });
        Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("Themes/Controls.xaml", UriKind.Relative) });
    }

    // ── Global exception handlers (prevent silent crashes) ──────────

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"[Dispatcher] {e.Exception}");
        MessageBox.Show($"Unhandled error:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "TrayZen Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Debug.WriteLine($"[Domain] {ex}");
            MessageBox.Show($"Fatal error:\n\n{ex.Message}", "TrayZen Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Debug.WriteLine($"[Task] {e.Exception}");
        e.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _hotkeyService?.Dispose();
        _autoHideService?.Dispose();
        (_sp as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}
