using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CrosshairOverlay.Models;

public enum CrosshairMode
{
    Vector,
    Image
}

public class CrosshairDef : INotifyPropertyChanged
{
    private CrosshairMode _mode = CrosshairMode.Vector;
    private ObservableCollection<VectorLayer> _layers = new();
    private string? _imagePath;
    private double _imageWidth = 32;
    private double _imageHeight = 32;
    private double _opacity = 1.0;

    public CrosshairMode Mode { get => _mode; set => Set(ref _mode, value); }
    public ObservableCollection<VectorLayer> Layers { get => _layers; set => Set(ref _layers, value); }
    public string? ImagePath { get => _imagePath; set => Set(ref _imagePath, value); }
    public double ImageWidth { get => _imageWidth; set => Set(ref _imageWidth, value); }
    public double ImageHeight { get => _imageHeight; set => Set(ref _imageHeight, value); }
    public double Opacity { get => _opacity; set => Set(ref _opacity, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public CrosshairDef Clone()
    {
        var d = new CrosshairDef
        {
            Mode = Mode,
            ImagePath = ImagePath,
            ImageWidth = ImageWidth,
            ImageHeight = ImageHeight,
            Opacity = Opacity,
        };
        foreach (var l in Layers) d.Layers.Add(l.Clone());
        return d;
    }
}
