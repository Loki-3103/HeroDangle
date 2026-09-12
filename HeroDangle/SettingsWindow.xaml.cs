using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HeroDangle;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly HotkeyManager _hotkey;
    private readonly Action<CharmDefinition> _applyCharm;

    public SettingsWindow(AppConfig config, HotkeyManager hotkey, Action<CharmDefinition> applyCharm)
    {
        _config = config;
        _hotkey = hotkey;
        _applyCharm = applyCharm;
        InitializeComponent();
        HotkeyText.Text = hotkey.DisplayText;
        BuildCharms();
    }

    private void BuildCharms()
    {
        CharmPanel.Children.Clear();
        foreach (var charm in CharmCatalog.BuiltIn)
        {
            var button = new Button
            {
                Content = $"{charm.Emoji}  {charm.Name}",
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 6, 10, 6)
            };
            button.Click += (_, _) =>
            {
                _applyCharm(charm);
                StatusText.Text = $"Charm set to {charm.Name}.";
            };
            CharmPanel.Children.Add(button);
        }
    }

    private void OnHotkeyBoxMouseDown(object sender, MouseButtonEventArgs e)
    {
        HotkeyBox.Focus();
        StatusText.Text = "Listening for a shortcut…";
    }

    private void OnHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        uint mods = 0;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) mods |= NativeMethods.ModControl;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) mods |= NativeMethods.ModAlt;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) mods |= NativeMethods.ModShift;
        if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) mods |= NativeMethods.ModWin;

        if (mods == 0)
        {
            StatusText.Text = "Include Ctrl, Alt, Shift, or Win so it doesn't steal typing.";
            return;
        }

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!_hotkey.Rebind(mods, vk))
        {
            StatusText.Text = "That shortcut is already in use.";
            return;
        }

        _config.HotkeyModifiers = mods;
        _config.HotkeyVirtualKey = vk;
        ConfigService.Save(_config);
        HotkeyText.Text = _hotkey.DisplayText;
        StatusText.Text = $"Hotkey set to {_hotkey.DisplayText}.";
    }

    private void OnDone(object sender, RoutedEventArgs e) => Close();
}
