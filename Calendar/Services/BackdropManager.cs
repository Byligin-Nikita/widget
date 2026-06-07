using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Windows.UI;
using WinRT;

namespace Calendar.Services;

/// <summary>
/// Wraps a <see cref="DesktopAcrylicController"/> so the background tint and
/// see-through amount can be controlled at runtime (real, visible transparency,
/// unlike a fixed XAML backdrop).
/// </summary>
public sealed class BackdropManager
{
    private DesktopAcrylicController? _controller;
    private SystemBackdropConfiguration? _config;

    public bool IsActive => _controller is not null;

    public bool Attach(Window window)
    {
        if (!DesktopAcrylicController.IsSupported()) return false;

        _config = new SystemBackdropConfiguration { IsInputActive = true };
        _controller = new DesktopAcrylicController();
        _controller.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
        _controller.SetSystemBackdropConfiguration(_config);

        window.Activated += (_, args) =>
            _config.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        window.Closed += (_, _) =>
        {
            _controller?.Dispose();
            _controller = null;
        };
        return true;
    }

    public void Configure(Color tint, double opacity, bool isDark)
    {
        if (_controller is null || _config is null) return;

        _config.Theme = isDark ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light;
        opacity = Math.Clamp(opacity, 0.0, 1.0);

        _controller.TintColor = tint;
        _controller.FallbackColor = tint;
        // opacity 1 -> mostly solid tint; lower -> more of the blurred desktop shows.
        _controller.TintOpacity = (float)opacity;
        _controller.LuminosityOpacity = (float)Math.Clamp(opacity + 0.1, 0.0, 1.0);
    }
}
