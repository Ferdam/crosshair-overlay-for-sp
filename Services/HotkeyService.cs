using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace CrosshairOverlay.Services;

/// <summary>
/// Global hotkey via a background polling thread reading GetAsyncKeyState.
/// Fixed binding: Right Ctrl + Numpad 4. Requires NumLock on
/// (Numpad 4 with NumLock off reports as VK_LEFT and is intentionally ignored).
/// </summary>
public class HotkeyService : IDisposable
{
    private const int VK_RCONTROL = 0xA3;
    private const int VK_NUMPAD4 = 0x64;
    private const int PollIntervalMs = 16;

    private Thread? _thread;
    private volatile bool _running;
    private volatile bool _enabled = true;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public void Initialize()
    {
        if (_thread != null) return;
        _running = true;
        _thread = new Thread(PollLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
            Name = "HotkeyPoll",
        };
        _thread.Start();
    }

    private void PollLoop()
    {
        bool wasDown = false;
        while (_running)
        {
            if (_enabled)
            {
                bool rctrl = (GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0;
                bool num4 = (GetAsyncKeyState(VK_NUMPAD4) & 0x8000) != 0;
                bool isDown = rctrl && num4;
                if (isDown && !wasDown)
                {
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (App.Overlay.IsRunning) App.Overlay.Toggle();
                    }));
                }
                wasDown = isDown;
            }
            else
            {
                wasDown = false;
            }
            Thread.Sleep(PollIntervalMs);
        }
    }

    public void Dispose()
    {
        _running = false;
        _thread?.Join(200);
        _thread = null;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
