using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CrosshairOverlay.Models;
using CrosshairOverlay.Rendering;
using WinForms = System.Windows.Forms;

namespace CrosshairOverlay.Views;

public partial class SetupWindow : Window
{
    private Profile? _current;
    private VectorLayer? _currentLayer;
    private bool _suppressSync;
    private bool _userClosing;

    public SetupWindow()
    {
        InitializeComponent();

        TypeCombo.ItemsSource = Enum.GetValues(typeof(LayerPrimitive));
        ProfileList.ItemsSource = App.Profiles.Profiles;

        DetectedResText.Text = $"Detected: {(int)SystemParameters.PrimaryScreenWidth} × {(int)SystemParameters.PrimaryScreenHeight}";

        var active = App.Profiles.Active ?? App.Profiles.Profiles.FirstOrDefault();
        if (active != null)
        {
            ProfileList.SelectedItem = active;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_userClosing)
        {
            // X button — hide to tray instead of exiting
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }

    // ---------- Profile selection ----------

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _current = ProfileList.SelectedItem as Profile;
        App.Profiles.Active = _current;
        LoadFromProfile();
    }

    private void LoadFromProfile()
    {
        if (_current == null) return;
        _suppressSync = true;

        NameBox.Text = _current.Name;
        GlobalOpacityBox.Text = Fmt(_current.Crosshair.Opacity);
        ModeVectorRadio.IsChecked = _current.Crosshair.Mode == CrosshairMode.Vector;
        ModeImageRadio.IsChecked = _current.Crosshair.Mode == CrosshairMode.Image;

        ImagePathBox.Text = _current.Crosshair.ImagePath ?? "";
        ImageWidthBox.Text = Fmt(_current.Crosshair.ImageWidth);
        ImageHeightBox.Text = Fmt(_current.Crosshair.ImageHeight);

        OffsetXBox.Text = Fmt(_current.OffsetX);
        OffsetYBox.Text = Fmt(_current.OffsetY);
        AutoResCheck.IsChecked = _current.AutoResolution;
        CustomWidthBox.Text = Fmt(_current.CustomWidth);
        CustomHeightBox.Text = Fmt(_current.CustomHeight);

        LayerList.ItemsSource = _current.Crosshair.Layers;
        _currentLayer = _current.Crosshair.Layers.FirstOrDefault();
        LayerList.SelectedItem = _currentLayer;

        _suppressSync = false;

        LoadFromLayer();
        UpdateModeVisibility();
        RefreshPreview();
    }

    // ---------- Layer selection / editing ----------

    private void LayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentLayer = LayerList.SelectedItem as VectorLayer;
        LoadFromLayer();
    }

    private void LoadFromLayer()
    {
        if (_currentLayer == null)
        {
            LayerEditor.IsEnabled = false;
            return;
        }
        LayerEditor.IsEnabled = true;
        _suppressSync = true;

        LayerNameBox.Text = _currentLayer.Name;
        TypeCombo.SelectedItem = _currentLayer.Type;
        PrimaryColorBox.Text = _currentLayer.PrimaryColor;
        OutlineColorBox.Text = _currentLayer.OutlineColor;
        OutlineThicknessBox.Text = Fmt(_currentLayer.OutlineThickness);
        LineThicknessBox.Text = Fmt(_currentLayer.LineThickness);
        LineLengthBox.Text = Fmt(_currentLayer.LineLength);
        CenterGapBox.Text = Fmt(_currentLayer.CenterGap);
        DotDiameterBox.Text = Fmt(_currentLayer.DotDiameter);
        CircleDiameterBox.Text = Fmt(_currentLayer.CircleDiameter);
        RectWidthBox.Text = Fmt(_currentLayer.RectWidth);
        RectHeightBox.Text = Fmt(_currentLayer.RectHeight);
        LayerOffsetXBox.Text = Fmt(_currentLayer.OffsetX);
        LayerOffsetYBox.Text = Fmt(_currentLayer.OffsetY);
        LayerOpacityBox.Text = Fmt(_currentLayer.Opacity);

        _suppressSync = false;
        UpdateLayerRowVisibility();
    }

    private void UpdateLayerRowVisibility()
    {
        if (_currentLayer == null) return;
        var t = _currentLayer.Type;
        bool hasLines = t is LayerPrimitive.Cross or LayerPrimitive.TShape or LayerPrimitive.X or LayerPrimitive.Circle;
        bool hasGap = t is LayerPrimitive.Cross or LayerPrimitive.TShape or LayerPrimitive.X;
        bool hasLen = t is LayerPrimitive.Cross or LayerPrimitive.TShape or LayerPrimitive.X;
        LineThicknessRow.Visibility = hasLines ? Visibility.Visible : Visibility.Collapsed;
        LineLengthRow.Visibility = hasLen ? Visibility.Visible : Visibility.Collapsed;
        CenterGapRow.Visibility = hasGap ? Visibility.Visible : Visibility.Collapsed;
        DotDiameterRow.Visibility = t == LayerPrimitive.Dot ? Visibility.Visible : Visibility.Collapsed;
        CircleDiameterRow.Visibility = t == LayerPrimitive.Circle ? Visibility.Visible : Visibility.Collapsed;
        RectSizeRow.Visibility = t == LayerPrimitive.Rectangle ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateModeVisibility()
    {
        if (_current == null) return;
        bool vector = _current.Crosshair.Mode == CrosshairMode.Vector;
        LayersPanel.Visibility = vector ? Visibility.Visible : Visibility.Collapsed;
        LayerEditor.Visibility = vector ? Visibility.Visible : Visibility.Collapsed;
        ImageEditor.Visibility = vector ? Visibility.Collapsed : Visibility.Visible;
        CustomWidthBox.IsEnabled = !_current.AutoResolution;
        CustomHeightBox.IsEnabled = !_current.AutoResolution;
    }

    private void WriteBackProfile()
    {
        if (_current == null || _suppressSync) return;
        _current.Crosshair.Opacity = Clamp(ParseD(GlobalOpacityBox.Text, _current.Crosshair.Opacity), 0, 1);
        _current.Crosshair.ImagePath = string.IsNullOrWhiteSpace(ImagePathBox.Text) ? null : ImagePathBox.Text;
        _current.Crosshair.ImageWidth = ParseD(ImageWidthBox.Text, _current.Crosshair.ImageWidth);
        _current.Crosshair.ImageHeight = ParseD(ImageHeightBox.Text, _current.Crosshair.ImageHeight);
        _current.OffsetX = ParseD(OffsetXBox.Text, _current.OffsetX);
        _current.OffsetY = ParseD(OffsetYBox.Text, _current.OffsetY);
        _current.AutoResolution = AutoResCheck.IsChecked == true;
        _current.CustomWidth = ParseD(CustomWidthBox.Text, _current.CustomWidth);
        _current.CustomHeight = ParseD(CustomHeightBox.Text, _current.CustomHeight);
    }

    private void WriteBackLayer()
    {
        if (_currentLayer == null || _suppressSync) return;

        if (TypeCombo.SelectedItem is LayerPrimitive t) _currentLayer.Type = t;
        _currentLayer.PrimaryColor = PrimaryColorBox.Text;
        _currentLayer.OutlineColor = OutlineColorBox.Text;
        _currentLayer.OutlineThickness = ParseD(OutlineThicknessBox.Text, _currentLayer.OutlineThickness);
        _currentLayer.LineThickness = ParseD(LineThicknessBox.Text, _currentLayer.LineThickness);
        _currentLayer.LineLength = ParseD(LineLengthBox.Text, _currentLayer.LineLength);
        _currentLayer.CenterGap = ParseD(CenterGapBox.Text, _currentLayer.CenterGap);
        _currentLayer.DotDiameter = ParseD(DotDiameterBox.Text, _currentLayer.DotDiameter);
        _currentLayer.CircleDiameter = ParseD(CircleDiameterBox.Text, _currentLayer.CircleDiameter);
        _currentLayer.RectWidth = ParseD(RectWidthBox.Text, _currentLayer.RectWidth);
        _currentLayer.RectHeight = ParseD(RectHeightBox.Text, _currentLayer.RectHeight);
        _currentLayer.OffsetX = ParseD(LayerOffsetXBox.Text, _currentLayer.OffsetX);
        _currentLayer.OffsetY = ParseD(LayerOffsetYBox.Text, _currentLayer.OffsetY);
        _currentLayer.Opacity = Clamp(ParseD(LayerOpacityBox.Text, _currentLayer.Opacity), 0, 1);
    }

    // ---------- Generic change handlers ----------

    private void Editor_Changed(object sender, EventArgs e)
    {
        if (_suppressSync) return;
        WriteBackProfile();
        UpdateModeVisibility();
        RefreshPreview();
        App.Profiles.Save();
    }

    private void LayerEditor_Changed(object sender, EventArgs e)
    {
        if (_suppressSync) return;
        WriteBackLayer();
        UpdateLayerRowVisibility();
        RefreshLayerList();
        RefreshPreview();
        App.Profiles.Save();
    }

    private void LayerName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressSync || _currentLayer == null) return;
        _currentLayer.Name = LayerNameBox.Text;
        RefreshLayerList();
        App.Profiles.Save();
    }

    private void RefreshLayerList()
    {
        var sel = LayerList.SelectedItem;
        LayerList.Items.Refresh();
        LayerList.SelectedItem = sel;
    }

    private void AutoRes_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSync) return;
        WriteBackProfile();
        UpdateModeVisibility();
        RefreshPreview();
        App.Profiles.Save();
    }

    private void Mode_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressSync || _current == null) return;
        _current.Crosshair.Mode = ModeVectorRadio.IsChecked == true ? CrosshairMode.Vector : CrosshairMode.Image;
        UpdateModeVisibility();
        RefreshPreview();
        App.Profiles.Save();
    }

    private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressSync || _current == null) return;
        _current.Name = NameBox.Text;
        var selected = ProfileList.SelectedItem;
        ProfileList.Items.Refresh();
        ProfileList.SelectedItem = selected;
        App.Profiles.Save();
    }

    // ---------- Preview ----------

    private void RefreshPreview()
    {
        if (_current == null) return;
        if (PreviewCanvas.ActualWidth <= 0 || PreviewCanvas.ActualHeight <= 0) return;
        var cx = PreviewCanvas.ActualWidth / 2;
        var cy = PreviewCanvas.ActualHeight / 2;
        CrosshairFactory.Build(PreviewCanvas, cx, cy, _current.Crosshair);
    }

    private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => RefreshPreview();

    // ---------- Profile CRUD ----------

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var p = new Profile { Name = UniqueProfileName("New Profile") };
        p.Crosshair.Layers.Add(VectorLayer.CreateDefault(LayerPrimitive.Cross));
        App.Profiles.Profiles.Add(p);
        ProfileList.SelectedItem = p;
        App.Profiles.Save();
    }

    private void DuplicateProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        var p = _current.Clone();
        p.Name = UniqueProfileName(_current.Name + " (copy)");
        App.Profiles.Profiles.Add(p);
        ProfileList.SelectedItem = p;
        App.Profiles.Save();
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        if (App.Profiles.Profiles.Count <= 1)
        {
            MessageBox.Show("At least one profile must exist.", "Crosshair Overlay");
            return;
        }
        var idx = App.Profiles.Profiles.IndexOf(_current);
        App.Profiles.Profiles.Remove(_current);
        var next = App.Profiles.Profiles[Math.Min(idx, App.Profiles.Profiles.Count - 1)];
        ProfileList.SelectedItem = next;
        App.Profiles.Save();
    }

    private string UniqueProfileName(string baseName)
    {
        var name = baseName;
        int i = 2;
        while (App.Profiles.Profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{baseName} {i++}";
        }
        return name;
    }

    // ---------- Layer CRUD + reorder ----------

    private void AddLayerCross_Click(object sender, RoutedEventArgs e) => AddLayer(LayerPrimitive.Cross);
    private void AddLayerDot_Click(object sender, RoutedEventArgs e) => AddLayer(LayerPrimitive.Dot);
    private void AddLayerCircle_Click(object sender, RoutedEventArgs e) => AddLayer(LayerPrimitive.Circle);
    private void AddLayerT_Click(object sender, RoutedEventArgs e) => AddLayer(LayerPrimitive.TShape);
    private void AddLayerX_Click(object sender, RoutedEventArgs e) => AddLayer(LayerPrimitive.X);
    private void AddLayerRect_Click(object sender, RoutedEventArgs e) => AddLayer(LayerPrimitive.Rectangle);

    private void AddLayer(LayerPrimitive type)
    {
        if (_current == null) return;
        var layer = VectorLayer.CreateDefault(type);
        layer.Name = UniqueLayerName(type.ToString());
        _current.Crosshair.Layers.Add(layer);
        LayerList.SelectedItem = layer;
        RefreshPreview();
        App.Profiles.Save();
    }

    private string UniqueLayerName(string baseName)
    {
        if (_current == null) return baseName;
        var name = baseName;
        int i = 2;
        while (_current.Crosshair.Layers.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{baseName} {i++}";
        }
        return name;
    }

    private void LayerUp_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        if (sender is not FrameworkElement fe || fe.Tag is not VectorLayer layer) return;
        var idx = _current.Crosshair.Layers.IndexOf(layer);
        if (idx > 0)
        {
            _current.Crosshair.Layers.Move(idx, idx - 1);
            LayerList.SelectedItem = layer;
            RefreshPreview();
            App.Profiles.Save();
        }
    }

    private void LayerDown_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        if (sender is not FrameworkElement fe || fe.Tag is not VectorLayer layer) return;
        var idx = _current.Crosshair.Layers.IndexOf(layer);
        if (idx >= 0 && idx < _current.Crosshair.Layers.Count - 1)
        {
            _current.Crosshair.Layers.Move(idx, idx + 1);
            LayerList.SelectedItem = layer;
            RefreshPreview();
            App.Profiles.Save();
        }
    }

    private void LayerDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        if (sender is not FrameworkElement fe || fe.Tag is not VectorLayer layer) return;
        if (_current.Crosshair.Layers.Count <= 1)
        {
            MessageBox.Show("A vector profile must have at least one layer.", "Crosshair Overlay");
            return;
        }
        var idx = _current.Crosshair.Layers.IndexOf(layer);
        _current.Crosshair.Layers.Remove(layer);
        var next = _current.Crosshair.Layers[Math.Min(idx, _current.Crosshair.Layers.Count - 1)];
        LayerList.SelectedItem = next;
        RefreshPreview();
        App.Profiles.Save();
    }

    // ---------- Pickers / dialogs ----------

    private void PickPrimary_Click(object sender, RoutedEventArgs e) => PickColor(PrimaryColorBox);
    private void PickOutline_Click(object sender, RoutedEventArgs e) => PickColor(OutlineColorBox);

    private void PickColor(TextBox target)
    {
        using var dlg = new WinForms.ColorDialog { FullOpen = true, AnyColor = true };
        try
        {
            var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(target.Text);
            dlg.Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        }
        catch { /* keep default */ }

        if (dlg.ShowDialog() == WinForms.DialogResult.OK)
        {
            var c = dlg.Color;
            target.Text = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new WinForms.OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*",
            Title = "Select crosshair image",
        };
        if (dlg.ShowDialog() == WinForms.DialogResult.OK)
        {
            ImagePathBox.Text = dlg.FileName;
        }
    }

    // ---------- Bottom buttons ----------

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        WriteBackProfile();
        WriteBackLayer();
        App.Profiles.Save();
        App.Overlay.Start(_current);
        Hide();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _userClosing = true;
        App.Profiles.Save();
        Application.Current.Shutdown();
    }

    // ---------- Utils ----------

    private static string Fmt(double v) => v.ToString(CultureInfo.InvariantCulture);

    private static double ParseD(string s, double fallback)
    {
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out v)) return v;
        return fallback;
    }

    private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
}
