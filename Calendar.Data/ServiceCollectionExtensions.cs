using Calendar.Core.Repositories;
using Calendar.Core.Storage;
using Calendar.Data.Database;
using Calendar.Data.Repositories;
using Calendar.Data.Storage;
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
        services.AddSingleton<INoteRepository, NoteRepository>();
        services.AddSingleton<IAttachmentRepository, AttachmentRepository>();
        services.AddSingleton<IAttachmentStorage, AttachmentStorage>();
        return services;
    }
}
