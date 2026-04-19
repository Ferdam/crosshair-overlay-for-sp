using System;
using System.Windows;
using System.Windows.Interop;
using CrosshairOverlay.Interop;
using CrosshairOverlay.Models;
using CrosshairOverlay.Rendering;

namespace CrosshairOverlay.Views;

public partial class OverlayWindow : Window
{
    private Profile? _profile;

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_LAYERED
                 | NativeMethods.WS_EX_TRANSPARENT
                 | NativeMethods.WS_EX_TOOLWINDOW
                 | NativeMethods.WS_EX_NOACTIVATE
                 | NativeMethods.WS_EX_TOPMOST;
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);

        SizeToPrimaryScreen();
    }

    private void SizeToPrimaryScreen()
    {
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
    }

    public void Apply(Profile profile)
    {
        _profile = profile;
        if (IsLoaded) Render();
    }

    private void Render()
    {
        if (_profile == null) return;

        var screenW = SystemParameters.PrimaryScreenWidth;
        var screenH = SystemParameters.PrimaryScreenHeight;

        double refW = _profile.AutoResolution ? screenW : _profile.CustomWidth;
        double refH = _profile.AutoResolution ? screenH : _profile.CustomHeight;

        // Anchor to screen center regardless of reference resolution; offset is in reference units
        // Scale offset so custom-resolution offsets still translate to on-screen distance correctly
        double scaleX = _profile.AutoResolution ? 1 : screenW / refW;
        double scaleY = _profile.AutoResolution ? 1 : screenH / refH;

        double cx = screenW / 2 + _profile.OffsetX * scaleX;
        double cy = screenH / 2 + _profile.OffsetY * scaleY;

        CrosshairFactory.Build(OverlayCanvas, cx, cy, _profile.Crosshair);
    }
}
