namespace Calendar.Core.Models;

public enum AppTheme
{
    System = 0,
    Light = 1,
    Dark = 2
}

public sealed class AppSettings
{
    public int MainWindowX { get; set; } = 100;
    public int MainWindowY { get; set; } = 100;
    public int MainWindowWidth { get; set; } = 380;
    public int MainWindowHeight { get; set; } = 520;
    public string LastSection { get; set; } = "ClockDate";
    public bool AlwaysOnTop { get; set; }
    public bool Autostart { get; set; }
    public AppTheme Theme { get; set; } = AppTheme.System;

    public uint QuickAddModifiers { get; set; } = 0x0008 | 0x0002; // Win + Shift
    public uint QuickAddVirtualKey { get; set; } = 0x51; // Q
    public uint ToggleWidgetModifiers { get; set; } = 0x0008 | 0x0002;
    public uint ToggleWidgetVirtualKey { get; set; } = 0x50; // P
}
