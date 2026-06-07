using System.Text.Json.Serialization;

namespace Calendar.Core.Models;

public enum AppTheme
{
    System = 0,
    Light = 1,
    Dark = 2
}

public sealed class AppSettings
{
    // Win32 hotkey modifier flags
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    public int MainWindowX { get; set; } = 100;
    public int MainWindowY { get; set; } = 100;
    public int MainWindowWidth { get; set; } = 560;
    public int MainWindowHeight { get; set; } = 740;
    public string LastSection { get; set; } = "ClockDate";
    public bool AlwaysOnTop { get; set; }
    public bool Autostart { get; set; }
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>User-chosen accent (hex #RRGGBB). Background is auto-derived from it.</summary>
    public string AccentColor { get; set; } = "#2FA37C";

    /// <summary>Background opacity 0..1 (1 = opaque, lower = more see-through).</summary>
    public double BackgroundOpacity { get; set; } = 0.85;

    // Hotkeys are fixed for now (no config UI) -> not persisted, always taken from these defaults.
    [JsonIgnore] public uint QuickAddModifiers { get; set; } = ModWin | ModShift;      // Win + Shift
    [JsonIgnore] public uint QuickAddVirtualKey { get; set; } = 0x51;                  // Q
    [JsonIgnore] public uint ToggleWidgetModifiers { get; set; } = ModWin | ModShift;  // Win + Shift
    [JsonIgnore] public uint ToggleWidgetVirtualKey { get; set; } = 0x5A;              // Z (P is often taken)
}
