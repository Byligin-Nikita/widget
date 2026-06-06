using Calendar.Core.Models;
using Calendar.Core.Repositories;

namespace Calendar.Platform.Services;

public sealed class ReminderSchedulerService : IDisposable
{
    private readonly IReminderRepository _reminders;
    private readonly System.Timers.Timer _timer;
    private bool _checking;

    public event Action<ReminderItem>? ReminderDue;

    public ReminderSchedulerService(IReminderRepository reminders)
    {
        _reminders = reminders;
        _timer = new System.Timers.Timer(30_000);
        _timer.Elapsed += async (_, _) => await CheckAsync();
        _timer.AutoReset = true;
        _timer.Start();
    }

    public async Task CheckAsync()
    {
        if (_checking) return;
        _checking = true;
        try
        {
            var now = DateTime.Now;
            var pending = await _reminders.GetPendingAsync();
            foreach (var r in pending)
            {
                if (r.EffectiveTriggerAt <= now && !r.Notified)
                    ReminderDue?.Invoke(r);
            }
        }
        finally
        {
            _checking = false;
        }
    }

    public void Dispose() => _timer.Dispose();
}
