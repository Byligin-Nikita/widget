using Calendar.Core.Models;
using Calendar.Services;
using Calendar.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;

namespace Calendar.Pages;

public sealed partial class TasksPage : Page
{
    private List<TaskRowViewModel> _rows = [];

    public TasksPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ReloadAsync();
    }

    public async Task ReloadAsync() => await LoadTasksAsync();

    private async void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => await LoadTasksAsync();

    private async void AddTask_Click(object sender, RoutedEventArgs e)
    {
        var title = NewTaskBox.Text?.Trim();
        if (string.IsNullOrEmpty(title)) return;
        await AppHost.Tasks.SaveAsync(new TaskItem { Title = title, DueDate = DateTime.Today });
        NewTaskBox.Text = "";
        await LoadTasksAsync();
    }

    private async void TaskCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && TryGetTagGuid(cb.Tag, out var id))
        {
            var task = await AppHost.Tasks.GetByIdAsync(id);
            if (task is null) return;
            task.IsCompleted = cb.IsChecked == true;
            task.CompletionPercent = task.IsCompleted ? 100 : task.CompletionPercent;
            await AppHost.Tasks.SaveAsync(task);
            await LoadTasksAsync();
        }
    }

    private async void Progress_Changed(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is Slider slider && TryGetTagGuid(slider.Tag, out var id))
        {
            var task = await AppHost.Tasks.GetByIdAsync(id);
            if (task is null) return;
            task.CompletionPercent = (int)e.NewValue;
            task.IsCompleted = task.CompletionPercent >= 100;
            await AppHost.Tasks.SaveAsync(task);
            var row = _rows.FirstOrDefault(r => r.Id == id);
            if (row is not null)
            {
                row.CompletionPercent = task.CompletionPercent;
                row.IsCompleted = task.IsCompleted;
                row.UpdateDisplay();
            }
        }
    }

    private async Task LoadTasksAsync()
    {
        var all = await AppHost.Tasks.GetAllAsync();
        var filter = (FilterCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";

        var filtered = filter switch
        {
            "Today" => all.Where(t => t.DueDate?.Date == DateTime.Today.Date),
            "Done" => all.Where(t => t.IsCompleted),
            _ => all.Where(t => !t.IsCompleted)
        };

        _rows = filtered.Select(TaskRowViewModel.From).ToList();
        TasksList.ItemsSource = _rows;
        var empty = _rows.Count == 0;
        EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        TasksList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private static bool TryGetTagGuid(object? tag, out Guid id)
    {
        if (tag is Guid g)
        {
            id = g;
            return true;
        }
        if (tag is string s && Guid.TryParse(s, out id))
            return true;
        id = default;
        return false;
    }
}
