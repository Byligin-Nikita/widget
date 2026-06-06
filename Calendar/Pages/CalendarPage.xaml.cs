using Calendar.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Calendar.Pages;

public sealed partial class CalendarPage : Page
{
    public CalendarPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (MonthCalendar.SelectedDates.Count == 0)
            MonthCalendar.SelectedDates.Add(DateTime.Today);
        await LoadDayAsync();
    }

    private async void MonthCalendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        => await LoadDayAsync();

    private async Task LoadDayAsync()
    {
        if (MonthCalendar.SelectedDates.Count == 0) return;
        var date = MonthCalendar.SelectedDates[0].DateTime;

        var tasks = await AppHost.Tasks.GetByDueDateAsync(date);
        var reminders = await AppHost.Reminders.GetAllAsync();
        var dayReminders = reminders
            .Where(r => !r.IsDone && r.EffectiveTriggerAt.Date == date.Date)
            .Select(r => new DayListItem { Title = "⏰ " + r.Title })
            .ToList();

        var items = tasks.Select(t => new DayListItem { Title = t.IsCompleted ? "✓ " + t.Title : t.Title })
            .Concat(dayReminders)
            .ToList();

        DayItemsList.ItemsSource = items;
    }

    private sealed class DayListItem
    {
        public string Title { get; init; } = "";
    }
}
