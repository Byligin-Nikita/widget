using Calendar.Core.Repositories;
using Calendar.Data.Database;
using Calendar.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Calendar.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCalendarData(this IServiceCollection services)
    {
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<ITaskRepository, TaskRepository>();
        services.AddSingleton<IReminderRepository, ReminderRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        return services;
    }
}
