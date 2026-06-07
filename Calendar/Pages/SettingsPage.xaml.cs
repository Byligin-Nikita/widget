using System;
using Calendar.Core.Models;
using Calendar.Helpers;
using Calendar.Platform.Services;
using Calendar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;

namespace Calendar.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loading;

    public SettingsPage() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var s = App.CurrentSettings;

        _loading = true;
        AlwaysOnTopSwitch.IsOn = s.AlwaysOnTop;
        AutostartSwitch.IsOn = s.Autostart || AutostartService.IsEnabled();

        ThemeCombo.SelectedIndex = s.Theme switch
        {
            AppTheme.Light => 1,
            AppTheme.Dark => 2,
            _ => 0
        };

        var accent = ThemeManager.ParseHex(s.AccentColor, Color.FromArgb(0xFF, 0x2F, 0xA3, 0x7C));
        AccentSwatch.Background = new SolidColorBrush(accent);
        AccentPicker.Color = accent;

        OpacitySlider.Value = Math.Clamp(s.BackgroundOpacity, 0.4, 1.0) * 100;
        OpacityValue.Text = $"{(int)OpacitySlider.Value}%";
        _loading = false;
    }

    private async void AlwaysOnTopSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        App.CurrentSettings.AlwaysOnTop = AlwaysOnTopSwitch.IsOn;
        if (App.MainWidget is not null)
            WidgetWindowHelper.SetAlwaysOnTop(App.MainWidget, App.CurrentSettings.AlwaysOnTop);
        await App.SaveSettingsAsync();
    }

    private async void AutostartSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        App.CurrentSettings.Autostart = AutostartSwitch.IsOn;
        try { AutostartService.SetEnabled(AutostartSwitch.IsOn); }
        catch { /* ignore registry failures */ }
        await App.SaveSettingsAsync();
    }

    private async void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
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

    // ===== Appearance =====

    private void AccentPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_loading) return;
        ApplyAccent(args.NewColor);
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string hex)
        {
            var color = ThemeManager.ParseHex(hex, App.CurrentSettings.AccentColor is { } cur
                ? ThemeManager.ParseHex(cur, Color.FromArgb(0xFF, 0x2F, 0xA3, 0x7C))
                : Color.FromArgb(0xFF, 0x2F, 0xA3, 0x7C));
            AccentPicker.Color = color; // triggers ColorChanged -> ApplyAccent
        }
    }

    private void ApplyAccent(Color c)
    {
        App.CurrentSettings.AccentColor = ThemeManager.ToHex(c);
        AccentSwatch.Background = new SolidColorBrush(c);
        ThemeManager.ApplyFromSettings();
        // save deferred to flyout close to avoid a write per drag tick
    }

    private async void AccentFlyout_Closed(object sender, object e)
    {
        await App.SaveSettingsAsync();
    }

    private async void OpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_loading) return;
        App.CurrentSettings.BackgroundOpacity = e.NewValue / 100.0;
        OpacityValue.Text = $"{(int)e.NewValue}%";
        ThemeManager.ApplyFromSettings();
        await App.SaveSettingsAsync();
    }
}
