using Calendar.Controls;
using Calendar.Core.Models;
using Calendar.Services;
using Calendar.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace Calendar.Pages;

public sealed partial class TasksPage : Page
{
    private List<TaskRowViewModel> _rows = [];

    public TasksPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadTasksAsync();
    }

    public async Task ReloadAsync() => await LoadTasksAsync();

    private async void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Fires during InitializeComponent before TasksList exists; skip until ready.
        if (TasksList is null) return;
        await LoadTasksAsync();
    }

    private async void NewTaskBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await AddTaskAsync();
        }
    }

    private async void AddTask_Click(object sender, RoutedEventArgs e) => await AddTaskAsync();

    private async Task AddTaskAsync()
    {
        var title = NewTaskBox.Text?.Trim();
        if (string.IsNullOrEmpty(title)) return;
        await AppHost.Tasks.SaveAsync(new TaskItem { Title = title, DueDate = DateTime.Today });
        NewTaskBox.Text = "";
        await LoadTasksAsync();
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb || cb.Tag is not Guid id) return;
        var task = await AppHost.Tasks.GetByIdAsync(id);
        if (task is null) return;
        task.IsCompleted = cb.IsChecked == true;
        if (task.IsCompleted) task.CompletionPercent = 100;
        await AppHost.Tasks.SaveAsync(task);
        await LoadTasksAsync();
    }

    private async void Task_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TaskRowViewModel row)
            await EditTaskAsync(row.Id);
    }

    private async Task EditTaskAsync(Guid id)
    {
        var task = await AppHost.Tasks.GetByIdAsync(id);
        if (task is null) return;

        var box = new TextBox { Text = task.Title, Header = "Название" };
        var slider = new Slider { Minimum = 0, Maximum = 100, StepFrequency = 5, Value = task.CompletionPercent, Header = "Прогресс, %" };
        var done = new CheckBox { Content = "Выполнено", IsChecked = task.IsCompleted };
        var attachments = new AttachmentsControl { MinHeight = 150, Margin = new Thickness(0, 6, 0, 0) };

        var panel = new StackPanel { Spacing = 10, Width = 360 };
        panel.Children.Add(box);
        panel.Children.Add(slider);
        panel.Children.Add(done);
        panel.Children.Add(attachments);

        var dialog = new ContentDialog
        {
            Title = "Задача",
            Content = new ScrollViewer { Content = panel, MaxHeight = 480, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            PrimaryButtonText = "Сохранить",
            SecondaryButtonText = "Удалить",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        await attachments.LoadAsync(task.Id, AttachmentOwnerType.Task);

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            task.Title = string.IsNullOrWhiteSpace(box.Text) ? task.Title : box.Text.Trim();
            task.CompletionPercent = (int)slider.Value;
            task.IsCompleted = done.IsChecked == true || task.CompletionPercent >= 100;
            if (task.IsCompleted) task.CompletionPercent = 100;
            await AppHost.Tasks.SaveAsync(task);
        }
        else if (result == ContentDialogResult.Secondary)
        {
            await AppHost.Tasks.DeleteAsync(id);
        }

        // Refresh in all cases (attachments may have changed even on Cancel).
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        if (TasksList is null) return;

        var all = await AppHost.Tasks.GetAllAsync();
        var filter = (FilterCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Active";

        var filtered = filter switch
        {
            "Today" => all.Where(t => t.DueDate?.Date == DateTime.Today.Date && !t.IsCompleted),
            "Done" => all.Where(t => t.IsCompleted),
            _ => all.Where(t => !t.IsCompleted)
        };

        var list = filtered.ToList();
        var rows = new List<TaskRowViewModel>(list.Count);
        foreach (var t in list)
        {
            var hasAtt = (await AppHost.Attachments.GetForOwnerAsync(t.Id)).Count > 0;
            rows.Add(TaskRowViewModel.From(t, hasAtt));
        }
        _rows = rows;
        TasksList.ItemsSource = _rows;

        var empty = _rows.Count == 0;
        EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        TasksList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }
}
