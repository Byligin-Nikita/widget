using Calendar.Helpers;
using Calendar.Pages;
using H.NotifyIcon;
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
    private bool _resizing;
    private ResizeEdge _resizeEdge;
    private int _resizeStartW, _resizeStartH, _resizeStartX, _resizeStartY;
    private double _resizeStartPointerX, _resizeStartPointerY;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        var s = App.CurrentSettings;
        WidgetWindowHelper.ConfigureWidgetWindow(this, s.AlwaysOnTop, s.MainWindowX, s.MainWindowY, s.MainWindowWidth, s.MainWindowHeight);
        Closed += async (_, _) =>
        {
            WidgetWindowHelper.SaveBounds(this, App.CurrentSettings);
            await App.SaveSettingsAsync();
        };
    }

    public void ToggleVisibility()
    {
        if (Visible)
        {
            WidgetWindowHelper.SaveBounds(this, App.CurrentSettings);
            this.Hide();
        }
        else
        {
            this.Show();
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

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e) => this.Hide();

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

    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(RootGrid).Position;
        var size = new Size(RootGrid.ActualWidth, RootGrid.ActualHeight);
        _resizeEdge = WidgetWindowHelper.HitTestResize(pos, size);
        if (_resizeEdge == ResizeEdge.None) return;

        _resizing = true;
        _resizeStartPointerX = pos.X;
        _resizeStartPointerY = pos.Y;
        var bounds = WidgetWindowHelper.GetBounds(this);
        _resizeStartW = bounds.Width;
        _resizeStartH = bounds.Height;
        _resizeStartX = bounds.X;
        _resizeStartY = bounds.Y;
        RootGrid.CapturePointer(e.Pointer);
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizing) return;
        var pos = e.GetCurrentPoint(RootGrid).Position;
        var dx = pos.X - _resizeStartPointerX;
        var dy = pos.Y - _resizeStartPointerY;
        var x = _resizeStartX;
        var y = _resizeStartY;
        var w = _resizeStartW;
        var h = _resizeStartH;
        WidgetWindowHelper.Resize(this, _resizeEdge, dx, dy, ref w, ref h, ref x, ref y);
    }

    private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _resizing = false;
        _resizeEdge = ResizeEdge.None;
        RootGrid.ReleasePointerCapture(e.Pointer);
    }
}
