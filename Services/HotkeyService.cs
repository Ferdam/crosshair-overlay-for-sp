using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace CrosshairOverlay.Services;

/// <summary>
/// Global hotkey via a low-level keyboard hook so we can match the physical Right-Ctrl key
/// (vs. Left-Ctrl) and the physical Numpad-4 key (vs. Left-Arrow when NumLock is off).
/// Fixed binding: Right Ctrl + Numpad 4.
/// </summary>
public class HotkeyService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_RCONTROL = 0xA3;

    // Physical Numpad-4 scan code. The "extended" flag distinguishes it from the Left-Arrow
    // key which shares the same scan code.
    private const uint SCAN_NUMPAD4 = 0x4B;
    private const uint LLKHF_EXTENDED = 0x01;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc; // kept alive to prevent GC

    public void Initialize()
    {
        _proc = HookCallback;
        using var curProc = System.Diagnostics.Process.GetCurrentProcess();
        using var curMod = curProc.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curMod.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            bool extended = (data.flags & LLKHF_EXTENDED) != 0;

            if (data.scanCode == SCAN_NUMPAD4 && !extended)
            {
                // Physical Numpad 4 pressed — check if Right Ctrl is held.
                if ((GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0)
                {
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (App.Overlay.IsRunning) App.Overlay.Toggle();
                    }));
                }
            }
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        _proc = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
