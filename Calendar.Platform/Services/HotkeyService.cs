using System.Runtime.InteropServices;
using Calendar.Platform.Interop;

namespace Calendar.Platform.Services;

public enum HotkeyAction
{
    QuickAdd,
    ToggleWidget
}

public sealed class HotkeyService : IDisposable
{
    private IntPtr _hwnd = IntPtr.Zero;
    private NativeMethods.WndProcDelegate? _wndProc;
    private IntPtr _oldWndProc = IntPtr.Zero;

    public event Action<HotkeyAction>? HotkeyPressed;

    public void Register(uint quickAddMod, uint quickAddVk, uint toggleMod, uint toggleVk)
    {
        Unregister();
        EnsureMessageWindow();
        NativeMethods.RegisterHotKey(_hwnd, NativeMethods.HOTKEY_QUICK_ADD, quickAddMod, quickAddVk);
        NativeMethods.RegisterHotKey(_hwnd, NativeMethods.HOTKEY_TOGGLE_WIDGET, toggleMod, toggleVk);
    }

    public void Unregister()
    {
        if (_hwnd == IntPtr.Zero) return;
        NativeMethods.UnregisterHotKey(_hwnd, NativeMethods.HOTKEY_QUICK_ADD);
        NativeMethods.UnregisterHotKey(_hwnd, NativeMethods.HOTKEY_TOGGLE_WIDGET);
    }

    private void EnsureMessageWindow()
    {
        if (_hwnd != IntPtr.Zero) return;

        _wndProc = WindowProc;
        var hInstance = NativeMethods.GetModuleHandle(null);
        _hwnd = NativeMethods.CreateWindowEx(
            0, "Static", null, NativeMethods.WS_POPUP,
            0, 0, 0, 0,
            NativeMethods.HWND_MESSAGE,
            IntPtr.Zero, hInstance, IntPtr.Zero);

        _oldWndProc = NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_wndProc));
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            HotkeyPressed?.Invoke(id switch
            {
                NativeMethods.HOTKEY_QUICK_ADD => HotkeyAction.QuickAdd,
                NativeMethods.HOTKEY_TOGGLE_WIDGET => HotkeyAction.ToggleWidget,
                _ => HotkeyAction.ToggleWidget
            });
            return IntPtr.Zero;
        }

        return _oldWndProc != IntPtr.Zero
            ? NativeMethods.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam)
            : NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        Unregister();
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }
}
