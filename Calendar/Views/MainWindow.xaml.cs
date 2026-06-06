using Calendar.Helpers;
using Calendar.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Calendar.Views;

public sealed partial class MainWindow : Window
{
    private bool _dragging;
    private Point _dragStart;
    private int _winStartX, _winStartY;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        WidgetWindowHelper.SetWindowIcon(this);
        var s = App.CurrentSettings;
        var w = Math.Max(s.MainWindowWidth, 480);
        var h = Math.Max(s.MainWindowHeight, 640);
        WidgetWindowHelper.ConfigureWidgetWindow(this, s.AlwaysOnTop, s.MainWindowX, s.MainWindowY, w, h);
        Closed += async (_, _) =>
        {
            WidgetWindowHelper.SaveBounds(this, App.CurrentSettings);
            await App.SaveSettingsAsync();
        };
    }

    public void ToggleVisibility()
    {
        var aw = WidgetWindowHelper.GetAppWindow(this);
        if (aw.IsVisible)
        {
            WidgetWindowHelper.SaveBounds(this, App.CurrentSettings);
            aw.Hide();
        }
        else
        {
            aw.Show();
            Activate();
        }
    }

    public void NavigateToSection(string tag)
    {
        NavigateTo(tag);
        SelectNavItem(tag);
    }

    private void NavigateTo(string tag)
    {
        var page = tag switch
        {
            "ClockDate" => typeof(ClockDatePage),
            "Calendar" => typeof(CalendarPage),
            "Tasks" => typeof(TasksPage),
            "Reminders" => typeof(RemindersPage),
            "Notes" => typeof(NotesPage),
            "Settings" => typeof(SettingsPage),
            _ => typeof(ClockDatePage)
        };
        ContentFrame.Navigate(page);
        App.CurrentSettings.LastSection = tag;
        _ = App.SaveSettingsAsync();
    }

    private void SelectNavItem(string tag)
    {
        foreach (var item in NavView.MenuItems.Concat(NavView.FooterMenuItems))
        {
            if (item is NavigationViewItem nvi && nvi.Tag?.ToString() == tag)
            {
                NavView.SelectedItem = nvi;
                break;
            }
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            NavigateTo(tag);
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
            NavigateTo(tag);
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        => WidgetWindowHelper.GetAppWindow(this).Hide();

    public void RefreshCurrentPage()
    {
        if (ContentFrame.Content is TasksPage tp)
            _ = tp.ReloadAsync();
        else if (ContentFrame.Content is RemindersPage rp)
            _ = rp.ReloadAsync();
        else if (ContentFrame.Content is CalendarPage cp)
            ContentFrame.Navigate(typeof(CalendarPage));
    }

    private void DragBar_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _dragging = true;
        _dragStart = e.GetCurrentPoint(null).Position;
        var bounds = WidgetWindowHelper.GetBounds(this);
        _winStartX = bounds.X;
        _winStartY = bounds.Y;
        (sender as UIElement)?.CapturePointer(e.Pointer);
    }

    private void DragBar_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging) return;
        var pt = e.GetCurrentPoint(null).Position;
        var dx = (int)(pt.X - _dragStart.X);
        var dy = (int)(pt.Y - _dragStart.Y);
        WidgetWindowHelper.GetAppWindow(this).Move(new Windows.Graphics.PointInt32(_winStartX + dx, _winStartY + dy));
    }

    private void DragBar_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        (sender as UIElement)?.ReleasePointerCapture(e.Pointer);
    }
}
