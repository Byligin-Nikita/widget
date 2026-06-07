using System;
using Calendar.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Calendar.Services;

/// <summary>
/// Applies the user-chosen accent colour and background transparency at runtime
/// by mutating shared brushes (so live elements update). The background tint is
/// derived automatically from the accent.
/// </summary>
public static class ThemeManager
{
    private static readonly Color Fallback = Color.FromArgb(0xFF, 0x2F, 0xA3, 0x7C);

    public static Color Accent { get; private set; } = Fallback;

    public static void ApplyFromSettings()
    {
        var s = App.CurrentSettings;
        Apply(s.AccentColor, s.BackgroundOpacity, CurrentIsDark());
        App.MainWidget?.ApplyCaptionColors(Accent);
    }

    public static void Apply(string accentHex, double opacity, bool isDark)
    {
        var accent = ParseHex(accentHex, Fallback);
        Accent = accent;
        opacity = Math.Clamp(opacity, 0.4, 1.0);
        var alpha = (byte)Math.Round(opacity * 255);

        var res = Application.Current.Resources;

        SetBrush(res, "AccentBrush", accent);
        SetBrush(res, "AccentFillColorDefaultBrush", accent);
        SetBrush(res, "AccentFillColorSecondaryBrush", Shade(accent, -0.08));
        SetBrush(res, "AccentFillColorTertiaryBrush", Shade(accent, -0.15));

        var onAccent = Luminance(accent) > 0.62 ? Color.FromArgb(0xFF, 0x1F, 0x1F, 0x1F) : Colors.White;
        SetBrush(res, "TextOnAccentFillColorPrimaryBrush", onAccent);
        SetBrush(res, "TextOnAccentFillColorSecondaryBrush", onAccent);

        SetBrush(res, "HomeAccentBrush", accent);
        SetBrush(res, "HomeTintBrush", WithAlpha(accent, 0x2A));

        if (res.TryGetValue("OrbBrush", out var ob) && ob is LinearGradientBrush lg && lg.GradientStops.Count >= 3)
        {
            lg.GradientStops[0].Color = Shade(accent, 0.35);
            lg.GradientStops[1].Color = accent;
            lg.GradientStops[2].Color = Shade(accent, -0.25);
        }

        // Background + rail: a few tones of the accent, with the opacity applied.
        var darkBase = Color.FromArgb(0xFF, 0x0C, 0x10, 0x0E);
        var bg = isDark ? Mix(darkBase, accent, 0.16) : Mix(Colors.White, accent, 0.16);
        var rail = isDark ? Mix(darkBase, accent, 0.24) : Mix(Colors.White, accent, 0.09);
        SetBrush(res, "AppBackgroundBrush", WithAlpha(bg, alpha));
        SetBrush(res, "NavRailBrush", WithAlpha(rail, alpha));
    }

    private static bool CurrentIsDark()
    {
        var t = App.CurrentSettings.Theme;
        if (t == AppTheme.Dark) return true;
        if (t == AppTheme.Light) return false;
        if (App.MainWidget?.Content is FrameworkElement fe && fe.ActualTheme != ElementTheme.Default)
            return fe.ActualTheme == ElementTheme.Dark;
        return Application.Current.RequestedTheme == ApplicationTheme.Dark;
    }

    // ===== helpers =====

    private static void SetBrush(ResourceDictionary res, string key, Color color)
    {
        if (res.TryGetValue(key, out var existing) && existing is SolidColorBrush sb)
            sb.Color = color;
        else
            res[key] = new SolidColorBrush(color);
    }

    public static Color ParseHex(string hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        hex = hex.Trim().TrimStart('#');
        try
        {
            if (hex.Length == 6)
                return Color.FromArgb(0xFF,
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            if (hex.Length == 8)
                return Color.FromArgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
        }
        catch { /* invalid hex -> fallback */ }
        return fallback;
    }

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static Color WithAlpha(Color c, byte a) => Color.FromArgb(a, c.R, c.G, c.B);

    private static Color Mix(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(0xFF,
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }

    private static Color Shade(Color c, double amount)
        => amount >= 0 ? Mix(c, Colors.White, amount) : Mix(c, Colors.Black, -amount);

    private static double Luminance(Color c)
        => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
}
