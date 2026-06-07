using Calendar.Helpers;
using Calendar.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Calendar.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        var s = App.CurrentSettings;
        var w = Math.Max(s.MainWindowWidth, 480);
        var h = Math.Max(s.MainWindowHeight, 640);
        WidgetWindowHelper.ConfigureWidgetWindow(this, s.AlwaysOnTop, s.MainWindowX, s.MainWindowY, w, h);
        WidgetWindowHelper.SetWindowIcon(this);
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
        tag = Normalize(tag);
        NavigateTo(tag);
        SelectNavItem(tag);
    }

    private static string Normalize(string tag)
        => tag is "Tasks" or "Reminders" ? "Calendar" : tag;

    private void NavigateTo(string tag)
    {
        tag = Normalize(tag);
        var page = tag switch
        {
            "ClockDate" => typeof(ClockDatePage),
            "Calendar" => typeof(CalendarPage),
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

    public void RefreshCurrentPage()
    {
        if (ContentFrame.Content is NotesPage np)
            _ = np.ReloadAsync();
        else if (ContentFrame.Content is CalendarPage)
            ContentFrame.Navigate(typeof(CalendarPage));
    }
}
