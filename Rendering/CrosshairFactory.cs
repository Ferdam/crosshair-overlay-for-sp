using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CrosshairOverlay.Models;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;

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

    // Filled primitives: one shape, Fill=primary + Stroke=outline. Transparent fill → true hollow.

    private static void AddDot(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var d = l.DotDiameter;
        var ot = l.OutlineThickness;
        var dot = new Ellipse
        {
            Width = d,
            Height = d,
            Fill = primary,
            Stroke = ot > 0 ? outline : null,
            StrokeThickness = ot,
        };
        Canvas.SetLeft(dot, cx - d / 2);
        Canvas.SetTop(dot, cy - d / 2);
        canvas.Children.Add(dot);
    }

    private static void AddRectangle(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var w = l.RectWidth;
        var h = l.RectHeight;
        var ot = l.OutlineThickness;
        var rect = new System.Windows.Shapes.Rectangle
        {
            Width = w,
            Height = h,
            Fill = primary,
            Stroke = ot > 0 ? outline : null,
            StrokeThickness = ot,
        };
        Canvas.SetLeft(rect, cx - w / 2);
        Canvas.SetTop(rect, cy - h / 2);
        canvas.Children.Add(rect);
    }

    // Circle: a ring (donut) path. Fill fills the ring body; Stroke outlines *both* the inner
    // and outer edges of the ring, so a transparent primary leaves two concentric outlines.
    private static void AddCircle(Canvas canvas, double cx, double cy, VectorLayer l, Brush primary, Brush outline)
    {
        var d = l.CircleDiameter;
        var t = l.LineThickness;
        var ot = l.OutlineThickness;
        var outerR = d / 2 + t / 2;
        var innerR = Math.Max(0, d / 2 - t / 2);

        var outerEllipse = new EllipseGeometry(new Point(cx, cy), outerR, outerR);
        var innerEllipse = new EllipseGeometry(new Point(cx, cy), innerR, innerR);
        Geometry ring = innerR > 0
            ? new CombinedGeometry(GeometryCombineMode.Exclude, outerEllipse, innerEllipse)
            : outerEllipse;
        ring.Freeze();

        var path = new Path
        {
            Data = ring,
            Fill = primary,
            Stroke = ot > 0 ? outline : null,
            StrokeThickness = ot,
            SnapsToDevicePixels = true,
        };
        canvas.Children.Add(path);
    }

    // Line-based primitives: each segment becomes a filled rectangle polygon (rotated as needed)
    // so Fill+Stroke behave the same as for filled shapes — transparent fill → hollow stroke pair.

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
        // T's natural bbox: x ∈ [cx-len, cx+len], y ∈ [cy, cy+gap+len] (bar + stem).
        // Shift so bbox center sits exactly on (cx, cy).
        double shiftY = -(gap + len) / 2;
        AddSegment(canvas, cx - len, cy + shiftY, cx + len, cy + shiftY, l, primary, outline);
        AddSegment(canvas, cx, cy + gap + shiftY, cx, cy + gap + len + shiftY, l, primary, outline);
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

    // Draws a thick segment as a polygon so Fill/Stroke behave identically to the filled shapes.
    private static void AddSegment(Canvas canvas, double x1, double y1, double x2, double y2,
        VectorLayer l, Brush primary, Brush outline)
    {
        var t = l.LineThickness;
        var ot = l.OutlineThickness;
        double dx = x2 - x1, dy = y2 - y1;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len <= 0 || t <= 0) return;

        // Perpendicular vector of length t/2
        double px = -dy / len * (t / 2);
        double py = dx / len * (t / 2);

        var poly = new Polygon
        {
            Points = new PointCollection
            {
                new Point(x1 + px, y1 + py),
                new Point(x2 + px, y2 + py),
                new Point(x2 - px, y2 - py),
                new Point(x1 - px, y1 - py),
            },
            Fill = primary,
            Stroke = ot > 0 ? outline : null,
            StrokeThickness = ot,
            StrokeLineJoin = PenLineJoin.Miter,
            SnapsToDevicePixels = true,
        };
        canvas.Children.Add(poly);
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
