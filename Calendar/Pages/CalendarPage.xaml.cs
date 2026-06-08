using System.Globalization;
using Calendar.Core.Models;
using Calendar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace Calendar.Pages;

public sealed partial class CalendarPage : Page
{
    // Segoe Fluent / MDL2 glyphs by code point (avoids embedding the chars in source)
    private static readonly string GlyphTaskOpen = ((char)0xE739).ToString();   // empty checkbox
    private static readonly string GlyphTaskDone = ((char)0xE73A).ToString();   // checked checkbox
    private static readonly string GlyphReminder = ((char)0xE121).ToString();   // clock

    private static readonly CultureInfo Ru = new("ru-RU");
    private static readonly Brush Transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

    // Recomputed from the user's accent (ThemeManager.Accent) on each build.
    private Brush _accent = new SolidColorBrush(Color.FromArgb(0xFF, 0x2F, 0xA3, 0x7C));
    private Brush _selectedBg = new SolidColorBrush(Color.FromArgb(0x3A, 0x2F, 0xA3, 0x7C));
    private Brush _todayBg = new SolidColorBrush(Color.FromArgb(0x18, 0x2F, 0xA3, 0x7C));

    private void UpdateAccentBrushes()
    {
        var a = ThemeManager.Accent;
        _accent = new SolidColorBrush(a);
        _selectedBg = new SolidColorBrush(Color.FromArgb(0x3A, a.R, a.G, a.B));
        _todayBg = new SolidColorBrush(Color.FromArgb(0x18, a.R, a.G, a.B));
    }

    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _selected;
    private DateTime _detailDate;
    private readonly List<DayCell> _cells = [];

    public CalendarPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _selected = DateTime.Today;
        await BuildMonthAsync();
    }

    private async Task BuildMonthAsync()
    {
        UpdateAccentBrushes();
        MonthLabel.Text = Capitalize(_month.ToString("MMMM yyyy", Ru));
        DayGrid.Children.Clear();
        _cells.Clear();

        var first = new DateTime(_month.Year, _month.Month, 1);
        var offset = ((int)first.DayOfWeek + 6) % 7; // Monday = 0
        var start = first.AddDays(-offset);

        var withItems = await GetDaysWithItemsAsync(start, start.AddDays(42));

        for (var i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var cell = CreateCell(date, date.Month == _month.Month, date.Date == DateTime.Today.Date, withItems.Contains(date.Date));
            Grid.SetColumn(cell.Border, i % 7);
            Grid.SetRow(cell.Border, i / 7);
            DayGrid.Children.Add(cell.Border);
            _cells.Add(cell);
        }

        RefreshSelectionVisuals();
    }

    private DayCell CreateCell(DateTime date, bool currentMonth, bool isToday, bool hasItems)
    {
        var label = new TextBlock
        {
            Text = date.Day.ToString(),
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var dot = new Ellipse
        {
            Width = 5,
            Height = 5,
            Fill = _accent,
            Margin = new Thickness(0, 2, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed
        };
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(label);
        stack.Children.Add(dot);

        var border = new Border
        {
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(2),
            MinHeight = 42,
            Background = Transparent,
            Child = stack,
            Tag = date,
            Opacity = currentMonth ? 1.0 : 0.35
        };
        border.Tapped += Cell_Tapped;
        border.DoubleTapped += Cell_DoubleTapped;

        return new DayCell { Border = border, Date = date, IsToday = isToday };
    }

    private void RefreshSelectionVisuals()
    {
        foreach (var c in _cells)
        {
            var isSel = _selected.HasValue && c.Date.Date == _selected.Value.Date;
            c.Border.Background = isSel ? _selectedBg : (c.IsToday ? _todayBg : Transparent);
            c.Border.BorderBrush = c.IsToday ? _accent : Transparent;
            c.Border.BorderThickness = c.IsToday ? new Thickness(1.5) : new Thickness(0);
        }
    }

    private void Cell_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border b && b.Tag is DateTime date)
        {
            _selected = date;
            RefreshSelectionVisuals();
        }
    }

    private async void Cell_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is Border b && b.Tag is DateTime date)
        {
            _selected = date;
            RefreshSelectionVisuals();
            await OpenDetailAsync(date);
        }
    }

    private async Task OpenDetailAsync(DateTime date)
    {
        _detailDate = date;
        DetailPanel.Visibility = Visibility.Visible;
        DetailTitle.Text = Capitalize(date.ToString("d MMMM, dddd", Ru));
        await LoadDayItemsAsync(date);
    }

    private void CloseDetail_Click(object sender, RoutedEventArgs e)
        => DetailPanel.Visibility = Visibility.Collapsed;

    private async Task LoadDayItemsAsync(DateTime date)
    {
        var tasks = await AppHost.Tasks.GetByDueDateAsync(date);
        var reminders = (await AppHost.Reminders.GetAllAsync())
            .Where(r => !r.IsDone && r.EffectiveTriggerAt.Date == date.Date);

        var items = new List<DayItem>();
        items.AddRange(tasks.Select(t => new DayItem
        {
            Kind = DayItemKind.Task,
            Id = t.Id,
            Glyph = t.IsCompleted ? GlyphTaskDone : GlyphTaskOpen,
            Title = t.Title,
            Subtitle = t.IsCompleted ? "выполнено" : "задача"
        }));
        items.AddRange(reminders
            .OrderBy(r => r.EffectiveTriggerAt)
            .Select(r => new DayItem
            {
                Kind = DayItemKind.Reminder,
                Id = r.Id,
                Glyph = GlyphReminder,
                Title = r.Title,
                Subtitle = r.EffectiveTriggerAt.ToString("HH:mm")
            }));

        DayItemsList.ItemsSource = items;
        DayEmpty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task<HashSet<DateTime>> GetDaysWithItemsAsync(DateTime start, DateTime end)
    {
        var set = new HashSet<DateTime>();
        foreach (var t in await AppHost.Tasks.GetAllAsync())
            if (t.DueDate is { } due && due.Date >= start.Date && due.Date < end.Date)
                set.Add(due.Date);
        foreach (var r in await AppHost.Reminders.GetAllAsync())
            if (!r.IsDone)
            {
                var d = r.EffectiveTriggerAt.Date;
                if (d >= start.Date && d < end.Date) set.Add(d);
            }
        return set;
    }

    private async void Prev_Click(object sender, RoutedEventArgs e)
    {
        _month = _month.AddMonths(-1);
        await BuildMonthAsync();
    }

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        _month = _month.AddMonths(1);
        await BuildMonthAsync();
    }

    private async void Today_Click(object sender, RoutedEventArgs e)
    {
        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _selected = DateTime.Today;
        await BuildMonthAsync();
        await OpenDetailAsync(DateTime.Today);
    }

    // ===== Add =====

    private async void AddTask_Click(object sender, RoutedEventArgs e)
    {
        var box = new TextBox { PlaceholderText = "Название задачи" };
        if (await ShowDialogAsync("Новая задача", box, "Добавить") == ContentDialogResult.Primary
            && !string.IsNullOrWhiteSpace(box.Text))
        {
            await AppHost.Tasks.SaveAsync(new TaskItem { Title = box.Text.Trim(), DueDate = _detailDate });
            await RefreshAfterChangeAsync();
        }
    }

    private async void AddReminder_Click(object sender, RoutedEventArgs e)
    {
        var box = new TextBox { PlaceholderText = "Текст напоминания" };
        var time = new TimePicker { ClockIdentifier = "24HourClock", Time = new TimeSpan(9, 0, 0), Header = "Время" };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(box);
        panel.Children.Add(time);

        if (await ShowDialogAsync("Новое напоминание", panel, "Добавить") == ContentDialogResult.Primary
            && !string.IsNullOrWhiteSpace(box.Text))
        {
            await AppHost.Reminders.SaveAsync(new ReminderItem
            {
                Title = box.Text.Trim(),
                TriggerAt = _detailDate.Date.Add(time.Time)
            });
            await RefreshAfterChangeAsync();
        }
    }

    // ===== Edit =====

    private async void DayItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not DayItem item) return;
        if (item.Kind == DayItemKind.Task) await EditTaskAsync(item.Id);
        else await EditReminderAsync(item.Id);
    }

    private async Task EditTaskAsync(Guid id)
    {
        var task = await AppHost.Tasks.GetByIdAsync(id);
        if (task is null) return;

        var box = new TextBox { Text = task.Title, Header = "Название" };
        var done = new CheckBox { Content = "Выполнено", IsChecked = task.IsCompleted };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(box);
        panel.Children.Add(done);

        var result = await ShowDialogAsync("Задача", panel, "Сохранить", "Удалить");
        if (result == ContentDialogResult.Primary)
        {
            task.Title = string.IsNullOrWhiteSpace(box.Text) ? task.Title : box.Text.Trim();
            task.IsCompleted = done.IsChecked == true;
            task.CompletionPercent = task.IsCompleted ? 100 : task.CompletionPercent;
            await AppHost.Tasks.SaveAsync(task);
            await RefreshAfterChangeAsync();
        }
        else if (result == ContentDialogResult.Secondary)
        {
            await AppHost.Tasks.DeleteAsync(id);
            await RefreshAfterChangeAsync();
        }
    }

    private async Task EditReminderAsync(Guid id)
    {
        var rem = await AppHost.Reminders.GetByIdAsync(id);
        if (rem is null) return;

        var box = new TextBox { Text = rem.Title, Header = "Текст" };
        var time = new TimePicker { ClockIdentifier = "24HourClock", Time = rem.EffectiveTriggerAt.TimeOfDay, Header = "Время" };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(box);
        panel.Children.Add(time);

        var result = await ShowDialogAsync("Напоминание", panel, "Сохранить", "Удалить");
        if (result == ContentDialogResult.Primary)
        {
            rem.Title = string.IsNullOrWhiteSpace(box.Text) ? rem.Title : box.Text.Trim();
            rem.TriggerAt = _detailDate.Date.Add(time.Time);
            rem.SnoozeUntil = null;
            rem.Notified = false;
            await AppHost.Reminders.SaveAsync(rem);
            await RefreshAfterChangeAsync();
        }
        else if (result == ContentDialogResult.Secondary)
        {
            await AppHost.Reminders.DeleteAsync(id);
            await RefreshAfterChangeAsync();
        }
    }

    private async Task RefreshAfterChangeAsync()
    {
        await BuildMonthAsync();
        if (DetailPanel.Visibility == Visibility.Visible)
            await LoadDayItemsAsync(_detailDate);
    }

    private async Task<ContentDialogResult> ShowDialogAsync(string title, UIElement content, string primary, string? secondary = null)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primary,
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };
        if (secondary is not null) dialog.SecondaryButtonText = secondary;
        return await dialog.ShowAsync();
    }

    private static string Capitalize(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Ru) + s[1..];

    private sealed class DayCell
    {
        public required Border Border { get; init; }
        public DateTime Date { get; init; }
        public bool IsToday { get; init; }
    }
}

public enum DayItemKind { Task, Reminder }

public sealed class DayItem
{
    public DayItemKind Kind { get; set; }
    public Guid Id { get; set; }
    public string Glyph { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public Visibility SubtitleVisibility =>
        string.IsNullOrEmpty(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
}
