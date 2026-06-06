using Calendar.Core.Models;
using Calendar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Calendar.Pages;

public sealed partial class RemindersPage : Page
{
    public RemindersPage()
    {
        InitializeComponent();
        DatePicker.Date = DateTime.Today;
        TimePicker.Time = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(15));
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ReloadAsync();
    }

    public async Task ReloadAsync() => await LoadAsync();

    private async void AddReminder_Click(object sender, RoutedEventArgs e)
    {
        var title = NewReminderBox.Text?.Trim();
        if (string.IsNullOrEmpty(title)) return;

        var date = (DatePicker.Date ?? DateTimeOffset.Now).DateTime;
        var time = TimePicker.Time;
        var trigger = date.Add(time);

        await AppHost.Reminders.SaveAsync(new ReminderItem
        {
            Title = title,
            TriggerAt = trigger
        });

        NewReminderBox.Text = "";
        await LoadAsync();
    }

    private async void Snooze5_Click(object sender, RoutedEventArgs e) => await SnoozeAsync(sender, 5);
    private async void Snooze15_Click(object sender, RoutedEventArgs e) => await SnoozeAsync(sender, 15);
    private async void Snooze30_Click(object sender, RoutedEventArgs e) => await SnoozeAsync(sender, 30);

    private async Task SnoozeAsync(object sender, int minutes)
    {
        if (sender is Button btn && TryGetTagGuid(btn.Tag, out var id))
        {
            var r = await AppHost.Reminders.GetByIdAsync(id);
            if (r is null) return;
            r.SnoozeUntil = DateTime.Now.AddMinutes(minutes);
            r.Notified = false;
            await AppHost.Reminders.SaveAsync(r);
            await LoadAsync();
        }
    }

    private async void Done_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && TryGetTagGuid(btn.Tag, out var id))
        {
            var r = await AppHost.Reminders.GetByIdAsync(id);
            if (r is null) return;
            r.IsDone = true;
            await AppHost.Reminders.SaveAsync(r);
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        var items = await AppHost.Reminders.GetPendingAsync();
        var rows = items.Select(r => new ReminderRow
        {
            Id = r.Id,
            Title = r.Title,
            WhenText = r.EffectiveTriggerAt.ToString("g")
        }).ToList();
        RemindersList.ItemsSource = rows;
        var empty = rows.Count == 0;
        EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        RemindersList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private static bool TryGetTagGuid(object? tag, out Guid id)
    {
        if (tag is Guid g) { id = g; return true; }
        if (tag is string s && Guid.TryParse(s, out id)) return true;
        id = default;
        return false;
    }
}
