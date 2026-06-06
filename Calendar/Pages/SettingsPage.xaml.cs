using Calendar.Core.Models;
using Calendar.Helpers;
using Calendar.Platform.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;

namespace Calendar.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var s = App.CurrentSettings;
        AlwaysOnTopSwitch.IsOn = s.AlwaysOnTop;
        AutostartSwitch.IsOn = s.Autostart || AutostartService.IsEnabled();

        ThemeCombo.SelectedIndex = s.Theme switch
        {
            AppTheme.Light => 1,
            AppTheme.Dark => 2,
            _ => 0
        };
    }

    private async void AlwaysOnTopSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        App.CurrentSettings.AlwaysOnTop = AlwaysOnTopSwitch.IsOn;
        if (App.MainWidget is not null)
            WidgetWindowHelper.SetAlwaysOnTop(App.MainWidget, App.CurrentSettings.AlwaysOnTop);
        await App.SaveSettingsAsync();
    }

    private async void AutostartSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        App.CurrentSettings.Autostart = AutostartSwitch.IsOn;
        try
        {
            var path = Package.Current.InstalledLocation.Path;
            var exe = Path.Combine(path, "Calendar.exe");
            if (!File.Exists(exe))
                exe = Environment.ProcessPath ?? exe;
            AutostartService.SetEnabled(AutostartSwitch.IsOn, exe);
        }
        catch
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
                AutostartService.SetEnabled(AutostartSwitch.IsOn, exe);
        }
        await App.SaveSettingsAsync();
    }

    private async void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is not ComboBoxItem item) return;
        App.CurrentSettings.Theme = item.Tag?.ToString() switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.System
        };
        App.ApplyTheme(App.CurrentSettings.Theme);
        await App.SaveSettingsAsync();
    }
}
