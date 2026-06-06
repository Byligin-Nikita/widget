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

    /// <summary>Optional diagnostic sink (set by the host app).</summary>
    public static Action<string>? Logger { get; set; }

    public void Register(uint quickAddMod, uint quickAddVk, uint toggleMod, uint toggleVk)
    {
        Unregister();
        EnsureMessageWindow();

        var ok1 = NativeMethods.RegisterHotKey(_hwnd, NativeMethods.HOTKEY_QUICK_ADD, quickAddMod, quickAddVk);
        Logger?.Invoke($"register QuickAdd mod=0x{quickAddMod:X} vk=0x{quickAddVk:X} -> {ok1} (err {Marshal.GetLastWin32Error()})");

        var ok2 = NativeMethods.RegisterHotKey(_hwnd, NativeMethods.HOTKEY_TOGGLE_WIDGET, toggleMod, toggleVk);
        Logger?.Invoke($"register Toggle mod=0x{toggleMod:X} vk=0x{toggleVk:X} -> {ok2} (err {Marshal.GetLastWin32Error()})");
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
            Logger?.Invoke($"WM_HOTKEY id={id}");
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
