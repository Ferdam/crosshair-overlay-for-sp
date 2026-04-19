using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CrosshairOverlay.Models;

public class Profile : INotifyPropertyChanged
{
    private string _name = "New Profile";
    private double _offsetX;
    private double _offsetY;
    private bool _autoResolution = true;
    private double _customWidth = 1920;
    private double _customHeight = 1080;
    private CrosshairDef _crosshair = new();

    public string Name { get => _name; set => Set(ref _name, value); }
    public double OffsetX { get => _offsetX; set => Set(ref _offsetX, value); }
    public double OffsetY { get => _offsetY; set => Set(ref _offsetY, value); }
    public bool AutoResolution { get => _autoResolution; set => Set(ref _autoResolution, value); }
    public double CustomWidth { get => _customWidth; set => Set(ref _customWidth, value); }
    public double CustomHeight { get => _customHeight; set => Set(ref _customHeight, value); }
    public CrosshairDef Crosshair { get => _crosshair; set => Set(ref _crosshair, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public Profile Clone() => new()
    {
        Name = Name,
        OffsetX = OffsetX,
        OffsetY = OffsetY,
        AutoResolution = AutoResolution,
        CustomWidth = CustomWidth,
        CustomHeight = CustomHeight,
        Crosshair = Crosshair.Clone(),
    };
}

public class ProfileStore
{
    public List<Profile> Profiles { get; set; } = new();
    public string? ActiveProfileName { get; set; }
}
