using Microsoft.Win32;

namespace Calendar.Platform.Services;

public static class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "CalendarWidget";

    /// <summary>
    /// Stable install location the autostart entry always points to, independent of where
    /// the currently running exe lives (build folder, etc.). Deploy a copy here.
    /// </summary>
    public static string CanonicalExePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CalendarWidget", "app", "Calendar.exe");

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(AppName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
            ?? throw new InvalidOperationException("Cannot open Run registry key");

        if (enabled)
            key.SetValue(AppName, $"\"{CanonicalExePath}\"");
        else
            key.DeleteValue(AppName, false);
    }
}
