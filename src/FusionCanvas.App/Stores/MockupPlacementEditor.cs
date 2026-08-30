using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace FusionCanvas.App.Stores;

/// <summary>Accessible image-space rectangle editor. It scales the provider image to fit the viewport (letterboxed) and performs no artwork rendering or composition.</summary>
public sealed class MockupPlacementEditor : Control
{
    public static readonly StyledProperty<double> PlacementXProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(PlacementX), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> PlacementYProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(PlacementY), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> PlacementWidthProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(PlacementWidth), 100, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> PlacementHeightProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(PlacementHeight), 100, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> ImageWidthProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(ImageWidth), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> ImageHeightProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(ImageHeight), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string?> ImagePathProperty = AvaloniaProperty.Register<MockupPlacementEditor, string?>(nameof(ImagePath));

    private const double HandleSize = 8;
    private const double HitPadding = 8;
    private const double HandleHitRadius = 14;

    private Point? _lastPoint;
    private bool _resizing;

    public MockupPlacementEditor()
    {
        Focusable = true;
        AutomationProperties.SetName(this, "Mockup design-area placement");
    }

    public double PlacementX { get => GetValue(PlacementXProperty); set => SetValue(PlacementXProperty, value); }
    public double PlacementY { get => GetValue(PlacementYProperty); set => SetValue(PlacementYProperty, value); }
    public double PlacementWidth { get => GetValue(PlacementWidthProperty); set => SetValue(PlacementWidthProperty, value); }
    public double PlacementHeight { get => GetValue(PlacementHeightProperty); set => SetValue(PlacementHeightProperty, value); }
    public double ImageWidth { get => GetValue(ImageWidthProperty); set => SetValue(ImageWidthProperty, value); }
    public double ImageHeight { get => GetValue(ImageHeightProperty); set => SetValue(ImageHeightProperty, value); }
    public string? ImagePath { get => GetValue(ImagePathProperty); set => SetValue(ImagePathProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ImageWidthProperty || change.Property == ImageHeightProperty || change.Property == ImagePathProperty)
        {
            ClampPlacement();
            InvalidateVisual();
        }
        else if (change.Property == PlacementXProperty || change.Property == PlacementYProperty ||
                 change.Property == PlacementWidthProperty || change.Property == PlacementHeightProperty)
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var imageRect = ImageRect();
        context.DrawRectangle(new SolidColorBrush(Color.FromRgb(35, 43, 55)), new Pen(Brushes.SlateGray, 1), imageRect);
        if (!HasImage()) return;
        try
        {
            if (!string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath))
                using (var bitmap = new Bitmap(ImagePath)) context.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height), imageRect);
        }
        catch { }
        var rectangle = DisplayRectangle(imageRect);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(45, 70, 150, 230)), new Pen(Brushes.DodgerBlue, 2), rectangle);
        context.DrawRectangle(Brushes.DodgerBlue, null, new Rect(rectangle.Right - HandleSize, rectangle.Bottom - HandleSize, HandleSize, HandleSize));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!HasImage()) return;
        var point = e.GetPosition(this);
        var rectangle = DisplayRectangle(ImageRect());
        if (!rectangle.Inflate(HitPadding).Contains(point)) return;
        Focus();
        _resizing = Math.Abs(point.X - rectangle.Right) <= HandleHitRadius && Math.Abs(point.Y - rectangle.Bottom) <= HandleHitRadius;
        _lastPoint = point;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_lastPoint is not Point previous) return;
        var scale = Scale();
        if (scale <= 0) return;
        var point = e.GetPosition(this);
        var dx = (point.X - previous.X) / scale;
        var dy = (point.Y - previous.Y) / scale;
        if (_resizing)
        {
            PlacementWidth = Math.Clamp(PlacementWidth + dx, 1, Math.Max(1, ImageWidth - PlacementX));
            PlacementHeight = Math.Clamp(PlacementHeight + dy, 1, Math.Max(1, ImageHeight - PlacementY));
        }
        else
        {
            PlacementX = Math.Clamp(PlacementX + dx, 0, Math.Max(0, ImageWidth - PlacementWidth));
            PlacementY = Math.Clamp(PlacementY + dy, 0, Math.Max(0, ImageHeight - PlacementHeight));
        }
        _lastPoint = point;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        ResetDrag();
        e.Pointer.Capture(null);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        ResetDrag();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!HasImage()) return;
        var dx = e.Key == Key.Left ? -1 : e.Key == Key.Right ? 1 : 0;
        var dy = e.Key == Key.Up ? -1 : e.Key == Key.Down ? 1 : 0;
        if (dx == 0 && dy == 0) return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            PlacementWidth = Math.Clamp(PlacementWidth + dx, 1, Math.Max(1, ImageWidth - PlacementX));
            PlacementHeight = Math.Clamp(PlacementHeight + dy, 1, Math.Max(1, ImageHeight - PlacementY));
        }
        else
        {
            PlacementX = Math.Clamp(PlacementX + dx, 0, Math.Max(0, ImageWidth - PlacementWidth));
            PlacementY = Math.Clamp(PlacementY + dy, 0, Math.Max(0, ImageHeight - PlacementHeight));
        }
        e.Handled = true;
    }

    private bool HasImage() => ImageWidth > 0 && ImageHeight > 0;

    private double Scale()
    {
        var imageWidth = ImageWidth;
        var imageHeight = ImageHeight;
        if (imageWidth <= 0 || imageHeight <= 0) return 0;
        return Math.Min(Bounds.Width / imageWidth, Bounds.Height / imageHeight);
    }

    private Rect ImageRect()
    {
        var scale = Scale();
        if (scale <= 0) return Bounds;
        var width = ImageWidth * scale;
        var height = ImageHeight * scale;
        return new Rect((Bounds.Width - width) / 2, (Bounds.Height - height) / 2, width, height);
    }

    private Rect DisplayRectangle(Rect imageRect)
    {
        var scale = Scale();
        if (scale <= 0) return imageRect;
        return new Rect(
            imageRect.X + PlacementX * scale,
            imageRect.Y + PlacementY * scale,
            PlacementWidth * scale,
            PlacementHeight * scale);
    }

    private void ClampPlacement()
    {
        if (!HasImage()) return;
        var imageWidth = Math.Max(1, ImageWidth);
        var imageHeight = Math.Max(1, ImageHeight);
        var width = Math.Min(Math.Max(1, PlacementWidth), imageWidth);
        var height = Math.Min(Math.Max(1, PlacementHeight), imageHeight);
        var x = Math.Clamp(PlacementX, 0, Math.Max(0, imageWidth - width));
        var y = Math.Clamp(PlacementY, 0, Math.Max(0, imageHeight - height));
        if (x != PlacementX) PlacementX = x;
        if (y != PlacementY) PlacementY = y;
        if (width != PlacementWidth) PlacementWidth = width;
        if (height != PlacementHeight) PlacementHeight = height;
    }

    private void ResetDrag()
    {
        _lastPoint = null;
        _resizing = false;
    }
}
