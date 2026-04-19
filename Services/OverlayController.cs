using CrosshairOverlay.Models;
using CrosshairOverlay.Views;

namespace CrosshairOverlay.Services;

public class OverlayController
{
    private OverlayWindow? _window;

    public bool IsVisible => _window != null && _window.IsVisible;
    public bool IsRunning => _window != null;

    public void Start(Profile profile)
    {
        if (_window == null)
        {
            _window = new OverlayWindow();
            _window.Closed += (_, _) => _window = null;
        }
        _window.Apply(profile);
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }

    public void Toggle()
    {
        if (_window == null) return;
        if (_window.IsVisible) _window.Hide();
        else _window.Show();
    }

    public void Apply(Profile profile)
    {
        _window?.Apply(profile);
    }
}
