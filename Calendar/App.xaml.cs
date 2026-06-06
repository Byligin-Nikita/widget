using Calendar.Core.Models;
using Calendar.Platform.Services;
using Calendar.Services;
using Calendar.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Calendar;

public partial class App : Application
{
    public static MainWindow? MainWidget { get; private set; }
    public static QuickAddWindow? QuickAdd { get; private set; }
    public static AppSettings CurrentSettings { get; private set; } = new();

    private static DispatcherQueue? _dispatcherQueue;

    internal static void Log(string msg)
    {
        try
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "calendar_startup.log");
            System.IO.File.AppendAllText(path, $"{DateTime.Now:O} {msg}{Environment.NewLine}");
        }
        catch { /* ignore */ }
    }

    public App()
    {
        Log("ctor: before InitializeComponent");
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Log("ctor: InitializeComponent THREW: " + ex);
            throw;
        }
        Log("ctor: after InitializeComponent");
        UnhandledException += (_, e) =>
        {
            Log("UnhandledException: " + e.Exception);
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Log("OnLaunched: start");
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            AppHost.Initialize();
            Log("OnLaunched: AppHost.Initialize ok");
            NotificationService.Initialize();
            Log("OnLaunched: NotificationService ok");

            CurrentSettings = await AppHost.Settings.GetAsync();
            Log("OnLaunched: settings loaded");

            MainWidget = new MainWindow();
            Log("OnLaunched: MainWindow created");
            QuickAdd = new QuickAddWindow();
            Log("OnLaunched: QuickAddWindow created");

            ApplyTheme(CurrentSettings.Theme);

            SetupHotkeys();
            SetupReminderScheduler();
            Log("OnLaunched: hotkeys+scheduler ok");

            MainWidget.Activate();
            Log("OnLaunched: MainWidget activated");

            _tray = new TrayService();
            _tray.Show();
            Log("OnLaunched: tray ok");

            if (CurrentSettings.Autostart == false && AutostartService.IsEnabled())
                CurrentSettings.Autostart = true;

            if (CurrentSettings.Autostart)
                EnableAutostartIfNeeded();

            NavigateToLastSection();
            Log("OnLaunched: done");
        }
        catch (Exception ex)
        {
            Log("OnLaunched THREW: " + ex);
            throw;
        }
    }

    private static void EnableAutostartIfNeeded()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
                AutostartService.SetEnabled(true, exe);
        }
        catch
        {
            // ignore
        }
    }

    private static TrayService? _tray;

    private static void NavigateToLastSection()
    {
        if (MainWidget is null) return;
        var tag = CurrentSettings.LastSection;
        MainWidget.NavigateToSection(tag);
    }

    public static async Task SaveSettingsAsync()
    {
        await AppHost.Settings.SaveAsync(CurrentSettings);
    }

    public static void ApplyTheme(AppTheme theme)
    {
        var elementTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        if (MainWidget?.Content is FrameworkElement mainRoot)
            mainRoot.RequestedTheme = elementTheme;
        if (QuickAdd?.Content is FrameworkElement quickRoot)
            quickRoot.RequestedTheme = elementTheme;
    }

    private static void SetupHotkeys()
    {
        var hotkeys = AppHost.Hotkeys;
        hotkeys.HotkeyPressed += action =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                switch (action)
                {
                    case HotkeyAction.QuickAdd:
                        QuickAdd?.Toggle();
                        break;
                    case HotkeyAction.ToggleWidget:
                        MainWidget?.ToggleVisibility();
                        break;
                }
            });
        };

        hotkeys.Register(
            CurrentSettings.QuickAddModifiers,
            CurrentSettings.QuickAddVirtualKey,
            CurrentSettings.ToggleWidgetModifiers,
            CurrentSettings.ToggleWidgetVirtualKey);
    }

    private static void SetupReminderScheduler()
    {
        AppHost.ReminderScheduler.ReminderDue += reminder =>
        {
            _dispatcherQueue?.TryEnqueue(async () =>
            {
                NotificationService.ShowReminder(reminder);
                reminder.Notified = true;
                await AppHost.Reminders.SaveAsync(reminder);
            });
        };
    }

}
