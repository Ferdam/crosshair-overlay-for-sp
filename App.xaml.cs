using System;
using System.Threading;
using System.Windows;
using CrosshairOverlay.Services;
using CrosshairOverlay.Views;

namespace CrosshairOverlay;

public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private TrayIconService? _tray;
    private HotkeyService? _hotkeys;

    public static ProfileService Profiles { get; private set; } = null!;
    public static OverlayController Overlay { get; private set; } = null!;
    public static SetupWindow? SetupWindow { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, "CrosshairOverlay.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Crosshair Overlay is already running.", "Crosshair Overlay",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        Profiles = new ProfileService();
        Profiles.Load();

        Overlay = new OverlayController();

        _tray = new TrayIconService();
        _tray.Initialize();

        _hotkeys = new HotkeyService();
        _hotkeys.Initialize();

        ShowSetup();
    }

    public static void ShowSetup()
    {
        if (SetupWindow == null)
        {
            SetupWindow = new SetupWindow();
            SetupWindow.Closed += (_, _) => SetupWindow = null;
        }
        if (!SetupWindow.IsVisible) SetupWindow.Show();
        if (SetupWindow.WindowState == WindowState.Minimized) SetupWindow.WindowState = WindowState.Normal;
        SetupWindow.Activate();
        SetupWindow.Topmost = true;
        SetupWindow.Topmost = false;
        SetupWindow.Focus();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _tray?.Dispose();
        Overlay?.Stop();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
