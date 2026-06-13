using Calendar.Views;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Calendar.Services;

public sealed class TrayService : IDisposable
{
    private readonly TaskbarIcon _trayIcon;

    public TrayService()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Планер"
        };

        TrySetIcon();
        _trayIcon.DoubleClickCommand = new RelayCommand(ShowMain);
        _trayIcon.ContextFlyout = BuildMenu();
    }

    private MenuFlyout BuildMenu()
    {
        var menu = new MenuFlyout();

        var show = new MenuFlyoutItem { Text = "Показать виджет" };
        show.Click += (_, _) => ShowMain();
        menu.Items.Add(show);

        var quick = new MenuFlyoutItem { Text = "Быстрое добавление" };
        quick.Click += (_, _) => App.QuickAdd?.Toggle();
        menu.Items.Add(quick);

        menu.Items.Add(new MenuFlyoutSeparator());

        var exit = new MenuFlyoutItem { Text = "Выход" };
        exit.Click += (_, _) =>
        {
            App.MainWidget?.PrepareExit();
            App.MainWidget?.Close();
            Application.Current.Exit();
        };
        menu.Items.Add(exit);

        return menu;
    }

    private void TrySetIcon()
    {
        try
        {
            var png = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
            if (!System.IO.File.Exists(png)) return;
            using var src = new System.Drawing.Bitmap(png);
            using var small = new System.Drawing.Bitmap(src, new System.Drawing.Size(32, 32));
            _trayIcon.Icon = System.Drawing.Icon.FromHandle(small.GetHicon());
        }
        catch
        {
            // tray will fall back to default icon
        }
    }

    private static void ShowMain()
    {
        if (App.MainWidget is null) return;
        Calendar.Helpers.WidgetWindowHelper.GetAppWindow(App.MainWidget).Show();
        App.MainWidget.Activate();
    }

    public void Show() => _trayIcon.ForceCreate();

    public void Dispose() => _trayIcon.Dispose();
}
