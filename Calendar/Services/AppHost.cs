using Calendar.Core.Repositories;
using Calendar.Core.Sync;
using Calendar.Data;
using Calendar.Platform.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Calendar.Services;

public static class AppHost
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Services => _provider
        ?? throw new InvalidOperationException("AppHost not initialized");

    public static void Initialize()
    {
        var services = new ServiceCollection();
        services.AddCalendarData();
        services.AddSingleton<ICloudSyncProvider, NoOpCloudSyncProvider>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<ReminderSchedulerService>();
        _provider = services.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Services.GetRequiredService<T>();

    public static ITaskRepository Tasks => Get<ITaskRepository>();
    public static IReminderRepository Reminders => Get<IReminderRepository>();
    public static ISettingsRepository Settings => Get<ISettingsRepository>();
    public static HotkeyService Hotkeys => Get<HotkeyService>();
    public static ReminderSchedulerService ReminderScheduler => Get<ReminderSchedulerService>();
}
