using System.Globalization;
using Calendar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Calendar.Pages;

public sealed partial class ClockDatePage : Page
{
    private static readonly CultureInfo Ru = new("ru-RU");
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public ClockDatePage()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => UpdateClock();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateClock();
        _timer.Start();
        await LoadSummaryAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _timer.Stop();
        base.OnNavigatedFrom(e);
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("HH:mm:ss");
        DateText.Text = $"{now.ToString("dddd, d MMMM yyyy", Ru)}";
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            var todayTasks = await AppHost.Tasks.GetByDueDateAsync(DateTime.Today);
            var pending = todayTasks.Count(t => !t.IsCompleted);
            CalendarSub.Text = pending == 0 ? "На сегодня задач нет" : $"Сегодня: {pending} {Plural(pending, "задача", "задачи", "задач")}";

            var notes = await AppHost.Notes.GetAllAsync();
            NotesSub.Text = notes.Count == 0 ? "Пока пусто" : $"Всего: {notes.Count} {Plural(notes.Count, "заметка", "заметки", "заметок")}";

            var reminders = await AppHost.Reminders.GetPendingAsync();
            var now = DateTime.Now;
            var next = reminders.Where(r => !r.IsDone && r.EffectiveTriggerAt >= now)
                                .OrderBy(r => r.EffectiveTriggerAt)
                                .FirstOrDefault();
            NextReminderText.Text = next is null
                ? "Ближайших напоминаний нет"
                : $"Ближайшее: {next.EffectiveTriggerAt.ToString("d MMM, HH:mm", Ru)} — {next.Title}";
        }
        catch
        {
            // summary is best-effort; never break the home page
        }
    }

    private static string Plural(int n, string one, string few, string many)
    {
        var mod100 = n % 100;
        var mod10 = n % 10;
        if (mod100 is >= 11 and <= 14) return many;
        return mod10 switch { 1 => one, >= 2 and <= 4 => few, _ => many };
    }

    private void Tile_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string tag) return;
        if (tag == "QuickAdd")
        {
            App.QuickAdd?.Toggle();
            return;
        }
        App.MainWidget?.NavigateToSection(tag);
    }
}
