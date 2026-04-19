using System;
using System.Drawing.Drawing2D;
using System.Windows;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace CrosshairOverlay.Services;

public class TrayIconService : IDisposable
{
    private WinForms.NotifyIcon? _notifyIcon;
    private WinForms.ToolStripMenuItem? _toggleItem;

    public void Initialize()
    {
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = CreateCrosshairIcon(),
            Visible = true,
            Text = "Crosshair Overlay",
        };

        var menu = new WinForms.ContextMenuStrip();
        var setupItem = new WinForms.ToolStripMenuItem("Open Setup");
        setupItem.Click += (_, _) => App.ShowSetup();

        _toggleItem = new WinForms.ToolStripMenuItem("Toggle Overlay")
        {
            ShortcutKeyDisplayString = "R-Ctrl + Num4",
        };
        _toggleItem.Click += (_, _) => App.Overlay.Toggle();

        var exitItem = new WinForms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Application.Current.Shutdown();

        menu.Items.Add(setupItem);
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        menu.Opening += (_, _) =>
        {
            if (_toggleItem != null)
            {
                _toggleItem.Enabled = App.Overlay.IsRunning;
                _toggleItem.Text = App.Overlay.IsVisible ? "Hide Overlay" : "Show Overlay";
            }
        };

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => App.ShowSetup();
    }

    private static Drawing.Icon CreateCrosshairIcon()
    {
        using var bmp = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Drawing.Color.Transparent);
            using var pen = new Drawing.Pen(Drawing.Color.LimeGreen, 2);
            g.DrawLine(pen, 16, 4, 16, 12);
            g.DrawLine(pen, 16, 20, 16, 28);
            g.DrawLine(pen, 4, 16, 12, 16);
            g.DrawLine(pen, 20, 16, 28, 16);
            using var dot = new Drawing.SolidBrush(Drawing.Color.LimeGreen);
            g.FillEllipse(dot, 14, 14, 4, 4);
        }
        var hIcon = bmp.GetHicon();
        return Drawing.Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
