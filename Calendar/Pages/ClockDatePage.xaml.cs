using System.Globalization;
using Calendar.Core.Models;
using Calendar.Core.Text;
using Calendar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

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
        DateText.Text = now.ToString("dddd, d MMMM yyyy", Ru);
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            var todayTasks = await AppHost.Tasks.GetByDueDateAsync(DateTime.Today);
            var pending = todayTasks.Count(t => !t.IsCompleted);
            CalendarSub.Text = pending == 0
                ? "На сегодня задач нет"
                : $"Сегодня: {pending} {Plural(pending, "задача", "задачи", "задач")}";

            var notes = await AppHost.Notes.GetAllAsync();
            NotesSub.Text = notes.Count == 0
                ? "Пока пусто"
                : $"Всего: {notes.Count} {Plural(notes.Count, "заметка", "заметки", "заметок")}";

            var reminders = await AppHost.Reminders.GetPendingAsync();
            var now = DateTime.Now;
            var next = reminders.Where(r => !r.IsDone && r.EffectiveTriggerAt >= now)
                                .OrderBy(r => r.EffectiveTriggerAt)
                                .FirstOrDefault();
            RemindersSub.Text = next is null
                ? "Нет ближайших"
                : $"{next.EffectiveTriggerAt.ToString("d MMM, HH:mm", Ru)} — {next.Title}";
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

    private async void QuickBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter) return;
        e.Handled = true;

        var raw = (QuickBox.Text ?? string.Empty).Trim();
        if (raw.Length == 0) return;

        var parsed = NaturalDateParser.Parse(raw, DateTime.Now);
        var title = string.IsNullOrWhiteSpace(parsed.CleanTitle) ? raw : parsed.CleanTitle;

        string msg;
        try
        {
            if (parsed.HasTime && parsed.When is { } when)
            {
                await AppHost.Reminders.SaveAsync(new ReminderItem { Title = title, TriggerAt = when });
                msg = $"✓ Напоминание: {when.ToString("d MMM, HH:mm", Ru)} — {title}";
            }
            else
            {
                var due = parsed.When?.Date ?? DateTime.Today;
                await AppHost.Tasks.SaveAsync(new TaskItem { Title = title, DueDate = due });
                msg = $"✓ Задача на {due.ToString("d MMM", Ru)}: {title}";
            }
        }
        catch
        {
            msg = "Не удалось сохранить";
        }

        QuickBox.Text = string.Empty;
        QuickHint.Text = msg;
        QuickHint.Visibility = Visibility.Visible;
        await LoadSummaryAsync();
    }

    private void Tile_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string tag) return;
        switch (tag)
        {
            case "QuickAdd":
                App.QuickAdd?.Toggle();
                break;
            case "Minimize":
                App.MainWidget?.ToggleVisibility();
                break;
            default:
                App.MainWidget?.NavigateToSection(tag);
                break;
        }
    }
}
