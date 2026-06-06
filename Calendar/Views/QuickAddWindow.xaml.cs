using Calendar.Core.Models;
using Calendar.Helpers;
using Calendar.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;

namespace Calendar.Views;

public sealed partial class QuickAddWindow : Window
{
    public QuickAddWindow()
    {
        InitializeComponent();
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        ModeTask.Checked += (_, _) => UpdateMode();
        ModeReminder.Checked += (_, _) => UpdateMode();

        var appWindow = WidgetWindowHelper.GetAppWindow(this);
        if (appWindow.Presenter is OverlappedPresenter p)
        {
            p.SetBorderAndTitleBar(true, true);
            p.IsResizable = true;
            p.IsMaximizable = false;
            p.IsAlwaysOnTop = true;
        }
        appWindow.Resize(new SizeInt32(460, 360));
        appWindow.IsShownInSwitchers = false;
    }

    private void UpdateMode()
    {
        var isReminder = ModeReminder.IsChecked == true;
        DelayCombo.Visibility = isReminder ? Visibility.Visible : Visibility.Collapsed;
        TitleBox.PlaceholderText = isReminder ? "Текст (необязательно)..." : "Название...";
        TitleBox.Focus(FocusState.Programmatic);
    }

    private void HideWindow() => WidgetWindowHelper.GetAppWindow(this).Hide();

    public void Toggle()
    {
        var aw = WidgetWindowHelper.GetAppWindow(this);
        if (aw.IsVisible)
        {
            aw.Hide();
            return;
        }

        TitleBox.Text = string.Empty;
        ModeTask.IsChecked = true;
        UpdateMode();
        aw.Show();
        Activate();
        TitleBox.Focus(FocusState.Programmatic);
    }

    private async void Save_Click(object sender, RoutedEventArgs e) => await SaveAsync();

    private void Cancel_Click(object sender, RoutedEventArgs e) => HideWindow();

    private async void TitleBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await SaveAsync();
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            HideWindow();
        }
    }

    private async Task SaveAsync()
    {
        if (ModeTask.IsChecked == true)
        {
            var title = TitleBox.Text?.Trim();
            if (string.IsNullOrEmpty(title)) return;

            await AppHost.Tasks.SaveAsync(new TaskItem
            {
                Title = title,
                DueDate = DateTime.Today
            });
        }
        else
        {
            var delays = new[] { 5, 15, 30, 60 };
            var idx = DelayCombo.SelectedIndex < 0 ? 1 : DelayCombo.SelectedIndex;
            var minutes = delays[Math.Min(idx, delays.Length - 1)];
            var title = $"Напоминание ({minutes} мин)";
            if (!string.IsNullOrWhiteSpace(TitleBox.Text))
                title = TitleBox.Text.Trim();

            await AppHost.Reminders.SaveAsync(new ReminderItem
            {
                Title = title,
                TriggerAt = DateTime.Now.AddMinutes(minutes)
            });
        }

        HideWindow();
        App.MainWidget?.RefreshCurrentPage();
    }
}
