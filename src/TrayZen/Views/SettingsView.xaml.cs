using System.Windows.Controls;
using System.Windows.Input;
using TrayZen.ViewModels;

namespace TrayZen.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void HotkeyDisplay_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.StartRecordingHotkeyCommand.Execute(null);
            HotkeyDisplay.Focus();
        }
    }

    private void HotkeyDisplay_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && vm.IsRecordingHotkey)
        {
            e.Handled = true;
            vm.RecordKey(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers);
        }
    }
}
