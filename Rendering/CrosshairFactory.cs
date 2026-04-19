using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CrosshairOverlay.Models;

namespace CrosshairOverlay.Rendering;

public static class CrosshairFactory
{
    public static void Build(Canvas canvas, double cx, double cy, CrosshairDef def)
    {
        canvas.Children.Clear();
        canvas.Opacity = Clamp(def.Opacity, 0, 1);

        if (def.Mode == CrosshairMode.Image)
        {
            AddImage(canvas, cx, cy, def);
            return;
        }

        foreach (var layer in def.Layers)
        {
            AddLayer(canvas, cx + layer.OffsetX, cy + layer.OffsetY, layer);
        }
    }

    private static void AddLayer(Canvas canvas, double cx, double cy, VectorLayer layer)
    {
        var primary = FreezeBrush(ParseColor(layer.PrimaryColor));
        var outline = FreezeBrush(ParseColor(layer.OutlineColor));
        var layerCanvas = new Canvas
        {
            Opacity = Clamp(layer.Opacity, 0, 1),
            IsHitTestVisible = false,
        };

        switch (layer.Type)
        {
            case LayerPrimitive.Dot:
                AddDot(layerCanvas, cx, cy, layer, primary, outline); break;
            case LayerPrimitive.Cross:
                AddCross(layerCanvas, cx, cy, layer, primary, outline); break;
            case LayerPrimitive.Circle:
                AddCircle(layerCanvas, cx, cy, layer, primary, outline); break;
            case LayerPrimitive.TShape:
                AddTShape(layerCanvas, cx, cy, layer, primary, outline); break;
            case LayerPrimitive.X:
                AddX(layerCanvas, cx, cy, layer, primary, outline); break;
            case LayerPrimitive.Rectangle:
                AddRectangle(layerCanvas, cx, cy, layer, primary, outline); break;
        }

        canvas.Children.Add(layerCanvas);
    }

    private static void AddDot(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var d = l.DotDiameter;
        var ot = l.OutlineThickness;
        if (ot > 0)
        {
            var outer = new Ellipse { Width = d + ot * 2, Height = d + ot * 2, Fill = outline };
            Canvas.SetLeft(outer, cx - (d / 2 + ot));
            Canvas.SetTop(outer, cy - (d / 2 + ot));
            canvas.Children.Add(outer);
        }
        var inner = new Ellipse { Width = d, Height = d, Fill = primary };
        Canvas.SetLeft(inner, cx - d / 2);
        Canvas.SetTop(inner, cy - d / 2);
        canvas.Children.Add(inner);
    }

    private static void AddCircle(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var d = l.CircleDiameter;
        var t = l.LineThickness;
        var ot = l.OutlineThickness;
        if (ot > 0)
        {
            var ring = new Ellipse
            {
                Width = d, Height = d,
                Stroke = outline, StrokeThickness = t + ot * 2,
            };
            Canvas.SetLeft(ring, cx - d / 2);
            Canvas.SetTop(ring, cy - d / 2);
            canvas.Children.Add(ring);
        }
        var inner = new Ellipse
        {
            Width = d, Height = d,
            Stroke = primary, StrokeThickness = t,
        };
        Canvas.SetLeft(inner, cx - d / 2);
        Canvas.SetTop(inner, cy - d / 2);
        canvas.Children.Add(inner);
    }

    private static void AddRectangle(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var w = l.RectWidth;
        var h = l.RectHeight;
        var ot = l.OutlineThickness;
        if (ot > 0)
        {
            var outer = new System.Windows.Shapes.Rectangle
            {
                Width = w + ot * 2,
                Height = h + ot * 2,
                Fill = outline,
            };
            Canvas.SetLeft(outer, cx - (w / 2 + ot));
            Canvas.SetTop(outer, cy - (h / 2 + ot));
            canvas.Children.Add(outer);
        }
        var inner = new System.Windows.Shapes.Rectangle
        {
            Width = w,
            Height = h,
            Fill = primary,
        };
        Canvas.SetLeft(inner, cx - w / 2);
        Canvas.SetTop(inner, cy - h / 2);
        canvas.Children.Add(inner);
    }

    private static void AddCross(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var gap = l.CenterGap;
        var len = l.LineLength;
        AddSegment(canvas, cx, cy - gap - len, cx, cy - gap, l, primary, outline);
        AddSegment(canvas, cx, cy + gap, cx, cy + gap + len, l, primary, outline);
        AddSegment(canvas, cx - gap - len, cy, cx - gap, cy, l, primary, outline);
        AddSegment(canvas, cx + gap, cy, cx + gap + len, cy, l, primary, outline);
    }

    private static void AddTShape(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var gap = l.CenterGap;
        var len = l.LineLength;
        AddSegment(canvas, cx - len, cy, cx + len, cy, l, primary, outline);
        AddSegment(canvas, cx, cy + gap, cx, cy + gap + len, l, primary, outline);
    }

    private static void AddX(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var gap = l.CenterGap;
        var len = l.LineLength;
        var diag = Math.Sqrt(2) / 2;
        double gx = gap * diag, gy = gap * diag;
        double lx = len * diag, ly = len * diag;
        AddSegment(canvas, cx - gx - lx, cy - gy - ly, cx - gx, cy - gy, l, primary, outline);
        AddSegment(canvas, cx + gx, cy + gy, cx + gx + lx, cy + gy + ly, l, primary, outline);
        AddSegment(canvas, cx - gx - lx, cy + gy + ly, cx - gx, cy + gy, l, primary, outline);
        AddSegment(canvas, cx + gx, cy - gy, cx + gx + lx, cy - gy - ly, l, primary, outline);
    }

    private static void AddSegment(Canvas canvas, double x1, double y1, double x2, double y2,
        VectorLayer l, Brush primary, Brush outline)
    {
        var t = l.LineThickness;
        var ot = l.OutlineThickness;
        if (ot > 0)
        {
            var outer = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = outline, StrokeThickness = t + ot * 2,
                StrokeStartLineCap = PenLineCap.Flat, StrokeEndLineCap = PenLineCap.Flat,
                SnapsToDevicePixels = true,
            };
            canvas.Children.Add(outer);
        }
        var line = new Line
        {
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            Stroke = primary, StrokeThickness = t,
            StrokeStartLineCap = PenLineCap.Flat, StrokeEndLineCap = PenLineCap.Flat,
            SnapsToDevicePixels = true,
        };
        canvas.Children.Add(line);
    }

    private static void AddImage(Canvas canvas, double cx, double cy, CrosshairDef def)
    {
        if (string.IsNullOrWhiteSpace(def.ImagePath) || !File.Exists(def.ImagePath)) return;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(def.ImagePath, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            var img = new Image
            {
                Source = bmp,
                Width = def.ImageWidth,
                Height = def.ImageHeight,
                Stretch = Stretch.Uniform,
            };
            Canvas.SetLeft(img, cx - def.ImageWidth / 2);
            Canvas.SetTop(img, cy - def.ImageHeight / 2);
            canvas.Children.Add(img);
        }
        catch
        {
            // Bad image file — silently skip
        }
    }

    private static Brush FreezeBrush(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static Color ParseColor(string s)
    {
        try { return (Color)ColorConverter.ConvertFromString(s); }
        catch { return Colors.LimeGreen; }
    }

    private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
}
