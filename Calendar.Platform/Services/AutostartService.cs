using Microsoft.Win32;

namespace Calendar.Platform.Services;

public static class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "CalendarWidget";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(AppName) is string;
    }

    public static void SetEnabled(bool enabled, string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
            ?? throw new InvalidOperationException("Cannot open Run registry key");

        if (enabled)
            key.SetValue(AppName, $"\"{exePath}\"");
        else
            key.DeleteValue(AppName, false);
    }
}
