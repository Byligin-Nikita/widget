using Calendar.Core.Models;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Calendar.Services;

public static class NotificationService
{
    private const string AppId = "CalendarWidget.Planner";
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        try
        {
            AppNotificationManager.Default.NotificationInvoked += (_, _) => { };
            AppNotificationManager.Default.Register();
            _initialized = true;
        }
        catch
        {
            // Toast may require packaged app on some systems
        }
    }

    public static void ShowReminder(ReminderItem reminder)
    {
        if (!_initialized) return;

        try
        {
            var notification = new AppNotificationBuilder()
                .AddText("Напоминание")
                .AddText(reminder.Title)
                .SetTimeStamp(DateTime.Now)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // Fallback: user sees in-app list
        }
    }
}
