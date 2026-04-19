using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CrosshairOverlay.Models;

public enum LayerPrimitive
{
    Dot,
    Cross,
    Circle,
    TShape,
    X,
    Rectangle
}

public class VectorLayer : INotifyPropertyChanged
{
    private string _name = "Layer";
    private LayerPrimitive _type = LayerPrimitive.Cross;
    private string _primaryColor = "#FF00FF00";
    private string _outlineColor = "#FF000000";
    private double _outlineThickness = 1;
    private double _lineThickness = 2;
    private double _lineLength = 10;
    private double _centerGap = 3;
    private double _dotDiameter = 4;
    private double _circleDiameter = 16;
    private double _rectWidth = 16;
    private double _rectHeight = 8;
    private double _offsetX;
    private double _offsetY;
    private double _opacity = 1.0;

    public string Name { get => _name; set => Set(ref _name, value); }
    public LayerPrimitive Type { get => _type; set => Set(ref _type, value); }
    public string PrimaryColor { get => _primaryColor; set => Set(ref _primaryColor, value); }
    public string OutlineColor { get => _outlineColor; set => Set(ref _outlineColor, value); }
    public double OutlineThickness { get => _outlineThickness; set => Set(ref _outlineThickness, value); }
    public double LineThickness { get => _lineThickness; set => Set(ref _lineThickness, value); }
    public double LineLength { get => _lineLength; set => Set(ref _lineLength, value); }
    public double CenterGap { get => _centerGap; set => Set(ref _centerGap, value); }
    public double DotDiameter { get => _dotDiameter; set => Set(ref _dotDiameter, value); }
    public double CircleDiameter { get => _circleDiameter; set => Set(ref _circleDiameter, value); }
    public double RectWidth { get => _rectWidth; set => Set(ref _rectWidth, value); }
    public double RectHeight { get => _rectHeight; set => Set(ref _rectHeight, value); }
    public double OffsetX { get => _offsetX; set => Set(ref _offsetX, value); }
    public double OffsetY { get => _offsetY; set => Set(ref _offsetY, value); }
    public double Opacity { get => _opacity; set => Set(ref _opacity, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public VectorLayer Clone() => new()
    {
        Name = Name,
        Type = Type,
        PrimaryColor = PrimaryColor,
        OutlineColor = OutlineColor,
        OutlineThickness = OutlineThickness,
        LineThickness = LineThickness,
        LineLength = LineLength,
        CenterGap = CenterGap,
        DotDiameter = DotDiameter,
        CircleDiameter = CircleDiameter,
        RectWidth = RectWidth,
        RectHeight = RectHeight,
        OffsetX = OffsetX,
        OffsetY = OffsetY,
        Opacity = Opacity,
    };

    public static VectorLayer CreateDefault(LayerPrimitive type) => new()
    {
        Type = type,
        Name = type.ToString(),
    };
}
