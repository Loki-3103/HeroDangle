using System.Windows;
using System.Windows.Interop;

namespace HeroDangle;

public sealed class HotkeyManager : IDisposable
{
    public const int HotkeyId = 0x3103;

    private readonly Window _window;
    private HwndSource? _source;
    private bool _registered;

    public event Action? Pressed;

    public uint Modifiers { get; private set; }
    public uint VirtualKey { get; private set; }

    public HotkeyManager(Window window, uint modifiers, uint virtualKey)
    {
        _window = window;
        Modifiers = modifiers;
        VirtualKey = virtualKey;
        _window.SourceInitialized += OnSourceInitialized;
        _window.Closed += (_, _) => Dispose();
    }

    public bool Rebind(uint modifiers, uint virtualKey)
    {
        Modifiers = modifiers;
        VirtualKey = virtualKey;
        Unregister();
        return Register();
    }

    public string DisplayText => Format(Modifiers, VirtualKey);

    public static string Format(uint modifiers, uint virtualKey)
    {
        var parts = new List<string>();
        if ((modifiers & NativeMethods.ModControl) != 0) parts.Add("Ctrl");
        if ((modifiers & NativeMethods.ModAlt) != 0) parts.Add("Alt");
        if ((modifiers & NativeMethods.ModShift) != 0) parts.Add("Shift");
        if ((modifiers & NativeMethods.ModWin) != 0) parts.Add("Win");
        parts.Add(KeyName(virtualKey));
        return string.Join("+", parts);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(_window);
        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(WndProc);
        Register();
    }

    private bool Register()
    {
        // Register against IntPtr.Zero so WM_HOTKEY goes to the thread
        // message queue — works even with WsExNoActivate overlay windows.
        _registered = NativeMethods.RegisterHotKey(
            IntPtr.Zero,
            HotkeyId,
            Modifiers | NativeMethods.ModNorepeat,
            VirtualKey);

        if (_registered)
            ComponentDispatcher.ThreadFilterMessage += OnThreadMessage;

        return _registered;
    }

    private void Unregister()
    {
        if (_registered)
        {
            ComponentDispatcher.ThreadFilterMessage -= OnThreadMessage;
            NativeMethods.UnregisterHotKey(IntPtr.Zero, HotkeyId);
        }
        _registered = false;
    }

    private void OnThreadMessage(ref MSG msg, ref bool handled)
    {
        if (handled) return;
        if (msg.message == NativeMethods.WmHotkey && msg.wParam.ToInt32() == HotkeyId)
        {
            Pressed?.Invoke();
            handled = true;
        }
    }

    // Keep WndProc hook for any future window-level messages.
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        return IntPtr.Zero;
    }

    private static string KeyName(uint vk)
    {
        if (vk >= 0x41 && vk <= 0x5A)
            return ((char)vk).ToString();
        if (vk >= 0x30 && vk <= 0x39)
            return ((char)vk).ToString();
        return $"Vk{vk:X2}";
    }

    public void Dispose()
    {
        Unregister();
        _source?.RemoveHook(WndProc);
        _source = null;
    }
}
