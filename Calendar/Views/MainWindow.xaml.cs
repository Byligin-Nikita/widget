using System;
using Calendar.Controls;
using Calendar.Helpers;
using Calendar.Pages;
using Microsoft.UI.Xaml;

namespace Calendar.Views;

public sealed partial class MainWindow : Window
{
    private readonly NavRailButton[] _navButtons;

    public MainWindow()
    {
        InitializeComponent();
        _navButtons = new[] { NavHome, NavCalendar, NavNotes, NavSettings };
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

    // Win+Shift+Q: summon the widget on Notes and start a fresh note.
    public void OpenNewNote()
    {
        var aw = WidgetWindowHelper.GetAppWindow(this);
        if (!aw.IsVisible) aw.Show();
        Activate();

        SelectNavItem("Notes");
        ContentFrame.Navigate(typeof(NotesPage), "new");
        App.CurrentSettings.LastSection = "Notes";
        _ = App.SaveSettingsAsync();
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
        tag = Normalize(tag);
        foreach (var b in _navButtons)
            b.IsSelected = b.Section == tag;
    }

    private void Nav_Selected(object sender, EventArgs e)
    {
        if (sender is NavRailButton b)
            NavigateToSection(b.Section);
    }

    public void RefreshCurrentPage()
    {
        if (ContentFrame.Content is NotesPage np)
            _ = np.ReloadAsync();
        else if (ContentFrame.Content is CalendarPage)
            ContentFrame.Navigate(typeof(CalendarPage));
    }
}
