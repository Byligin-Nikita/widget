using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Calendar.Pages;

public sealed partial class ClockDatePage : Page
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public ClockDatePage()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => UpdateClock();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateClock();
        _timer.Start();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _timer.Stop();
        base.OnNavigatedFrom(e);
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var culture = new CultureInfo("ru-RU");
        TimeText.Text = now.ToString("HH:mm:ss");
        DateText.Text = now.ToString("d MMMM yyyy", culture);
        DayText.Text = culture.DateTimeFormat.GetDayName(now.DayOfWeek);
    }
}
