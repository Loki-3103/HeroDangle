using System.Windows;
using System.Windows.Interop;

namespace HeroDangle;

internal static class OverlayWindow
{
    public static void ApplyChrome(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        int ex = NativeMethods.GetWindowLong(hwnd, NativeMethods.GwlExStyle);
        ex |= NativeMethods.WsExLayered | NativeMethods.WsExToolwindow | NativeMethods.WsExNoActivate | NativeMethods.WsExTransparent;
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GwlExStyle, ex);
    }

    public static void SetClickThrough(Window window, bool clickThrough)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        int ex = NativeMethods.GetWindowLong(hwnd, NativeMethods.GwlExStyle);
        int next = clickThrough
            ? ex | NativeMethods.WsExTransparent
            : ex & ~NativeMethods.WsExTransparent;

        if (next != ex)
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GwlExStyle, next);
    }
}
