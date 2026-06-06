using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.Graphics;
using WinRT.Interop;

namespace Calendar.Helpers;

public enum ResizeEdge
{
    None, Left, Right, Top, Bottom,
    TopLeft, TopRight, BottomLeft, BottomRight
}

public static class WidgetWindowHelper
{
    private const int ResizeGripSize = 8;
    private const int MinWidth = 420;
    private const int MinHeight = 560;

    public static AppWindow GetAppWindow(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    public static void ConfigureWidgetWindow(Window window, bool alwaysOnTop, int x, int y, int width, int height)
    {
        var appWindow = GetAppWindow(window);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            // Native sizing border (drag edges/corners to resize) without an OS title bar.
            presenter.SetBorderAndTitleBar(true, false);
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
        }

        appWindow.IsShownInSwitchers = true;
        appWindow.MoveAndResize(new RectInt32(x, y, width, height));

        SetAlwaysOnTop(window, alwaysOnTop);

        TrySetMica(window);
    }

    public static void SetWindowIcon(Window window)
    {
        try
        {
            var ico = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (System.IO.File.Exists(ico))
                GetAppWindow(window).SetIcon(ico);
        }
        catch
        {
            // non-critical
        }
    }

    public static void SetAlwaysOnTop(Window window, bool onTop)
    {
        var appWindow = GetAppWindow(window);
        if (appWindow.Presenter is OverlappedPresenter overlapped)
            overlapped.IsAlwaysOnTop = onTop;
    }

    public static (int X, int Y, int Width, int Height) GetBounds(Window window)
    {
        var pos = GetAppWindow(window).Position;
        var size = GetAppWindow(window).Size;
        return (pos.X, pos.Y, size.Width, size.Height);
    }

    public static void SaveBounds(Window window, Core.Models.AppSettings settings)
    {
        var (x, y, w, h) = GetBounds(window);
        settings.MainWindowX = x;
        settings.MainWindowY = y;
        settings.MainWindowWidth = w;
        settings.MainWindowHeight = h;
    }

    public static ResizeEdge HitTestResize(Point position, Size windowSize)
    {
        var x = position.X;
        var y = position.Y;
        var w = windowSize.Width;
        var h = windowSize.Height;
        var g = ResizeGripSize;

        var left = x < g;
        var right = x > w - g;
        var top = y < g;
        var bottom = y > h - g;

        if (top && left) return ResizeEdge.TopLeft;
        if (top && right) return ResizeEdge.TopRight;
        if (bottom && left) return ResizeEdge.BottomLeft;
        if (bottom && right) return ResizeEdge.BottomRight;
        if (top) return ResizeEdge.Top;
        if (bottom) return ResizeEdge.Bottom;
        if (left) return ResizeEdge.Left;
        if (right) return ResizeEdge.Right;
        return ResizeEdge.None;
    }

    public static void Resize(Window window, ResizeEdge edge, double deltaX, double deltaY, ref int startW, ref int startH, ref int startX, ref int startY)
    {
        var appWindow = GetAppWindow(window);
        var newW = startW;
        var newH = startH;
        var newX = startX;
        var newY = startY;

        switch (edge)
        {
            case ResizeEdge.Right:
                newW = Math.Max(MinWidth, startW + (int)deltaX);
                break;
            case ResizeEdge.Bottom:
                newH = Math.Max(MinHeight, startH + (int)deltaY);
                break;
            case ResizeEdge.Left:
                newW = Math.Max(MinWidth, startW - (int)deltaX);
                newX = startX + (int)deltaX;
                break;
            case ResizeEdge.Top:
                newH = Math.Max(MinHeight, startH - (int)deltaY);
                newY = startY + (int)deltaY;
                break;
            case ResizeEdge.BottomRight:
                newW = Math.Max(MinWidth, startW + (int)deltaX);
                newH = Math.Max(MinHeight, startH + (int)deltaY);
                break;
            case ResizeEdge.BottomLeft:
                newW = Math.Max(MinWidth, startW - (int)deltaX);
                newH = Math.Max(MinHeight, startH + (int)deltaY);
                newX = startX + (int)deltaX;
                break;
            case ResizeEdge.TopRight:
                newW = Math.Max(MinWidth, startW + (int)deltaX);
                newH = Math.Max(MinHeight, startH - (int)deltaY);
                newY = startY + (int)deltaY;
                break;
            case ResizeEdge.TopLeft:
                newW = Math.Max(MinWidth, startW - (int)deltaX);
                newH = Math.Max(MinHeight, startH - (int)deltaY);
                newX = startX + (int)deltaX;
                newY = startY + (int)deltaY;
                break;
        }

        appWindow.MoveAndResize(new RectInt32(newX, newY, newW, newH));
    }

    private static void TrySetMica(Window window)
    {
        try
        {
            if (window.Content is FrameworkElement root)
            {
                root.RequestedTheme = ElementTheme.Default;
            }
        }
        catch
        {
            // Mica not critical
        }
    }
}
