# TrayZen

**TrayZen** is a Windows 10 tray icon laboratory in the form of a clean desktop utility.  
It treats the notification area as a controllable system surface: measurable, reproducible, and customizable.

If your tray feels noisy, TrayZen helps you restore signal from clutter.

## Why TrayZen?

System trays usually grow without governance. TrayZen applies a simple idea:

- define icon visibility as a state model,
- apply deterministic rules to that state,
- let users switch contexts (work/gaming/focus) in one action.

In short: less visual entropy, more intentional UI.

## Feature Set

- `Visibility Control` hide/show tray icons quickly
- `Custom Icon Replacement` override icon visuals with `.ico` assets
- `Zen Mode` one-click or hotkey-based focus mode
- `Smart Auto-Hide` idle-based and foreground-aware hiding rules
- `Icon Grouping` batch operations through named groups
- `Profiles` save/load/export/import full tray configurations
- `Native Tray Presence` TrayZen itself runs with tray icon + context menu
- `Minimize to Tray` close-to-tray workflow for persistent background use
- `Theme System` dark/light Fluent-inspired UI
- `Global Hotkeys` configurable recorder-based shortcut input

## System Model (How It Works)

TrayZen interacts with Windows Explorer's notification area toolbar:

```text
Shell_TrayWnd -> TrayNotifyWnd -> SysPager -> ToolbarWindow32 (visible tray)
NotifyIconOverflowWindow -> ToolbarWindow32 (overflow tray)
```

Core techniques:

- P/Invoke-based window discovery (`FindWindowExW`, `SendMessageW`)
- cross-process memory access (`OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`)
- toolbar inspection (`TBBUTTON`, embedded `TRAYDATA`)
- shell state updates (`Shell_NotifyIcon` + state flags)

This hybrid strategy balances compatibility (shell-level APIs) with deep control (toolbar-level interop).

## Technical Stack

- `.NET 10` + `WPF`
- `MVVM` with `CommunityToolkit.Mvvm`
- `Microsoft.Extensions.DependencyInjection` for composition
- `H.NotifyIcon.Wpf` for TrayZen's own tray lifecycle
- Win32 interop (`User32`, `Shell32`, `Kernel32`)

## Project Structure

```text
src/TrayZen/
  Native/        Win32 constants, structs, P/Invoke, tray interop
  Models/        tray icon/profile/settings models
  Services/      icon polling, profiles, settings, hotkeys, auto-hide
  ViewModels/    state + command orchestration (MVVM)
  Views/         XAML pages (Dashboard, Profiles, Settings)
  Themes/        dark/light resources and control styles
  Converters/    UI binding converters
```

## Quick Start

### Requirements

- Windows 10 (recommended: 19041+)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build and Run

```powershell
cd src/TrayZen
dotnet restore
dotnet build
dotnet run
```

> Note: some tray operations may require elevated permissions, depending on target processes and Explorer state.

## Design Principles

- **Deterministic behavior**: operations should be predictable and reversible
- **Minimal interface**: fewer controls, clearer intent
- **Separation of concerns**: interop in `Native`, policy in `Services`, UX in `ViewModels/Views`
- **Fail-soft runtime**: the app should remain usable even if a specific low-level operation fails

## Roadmap

- improve reliability for edge-case tray hosts
- refine icon grouping into richer folder-like interactions
- ship MSIX packaging and broader test coverage
- document interoperability boundaries across Windows builds

## Contributing

Issues and PRs are welcome. Helpful contributions include:

- reproducible bug reports (Windows build + app steps + expected/actual behavior)
- stability fixes around Explorer/tray edge cases
- performance improvements in polling and UI updates
- tests and documentation polish

## License

Released under the license in [LICENSE](LICENSE).
