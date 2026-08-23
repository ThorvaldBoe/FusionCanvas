using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace FusionCanvas.App.Stores;

/// <summary>Accessible image-space rectangle editor. It intentionally performs no rendering or composition.</summary>
public sealed class MockupPlacementEditor : Control
{
    public static readonly StyledProperty<double> PlacementXProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(PlacementX), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> PlacementYProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(PlacementY), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> PlacementWidthProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(PlacementWidth), 100, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> PlacementHeightProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(PlacementHeight), 100, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> ImageWidthProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(ImageWidth), 1, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> ImageHeightProperty = AvaloniaProperty.Register<MockupPlacementEditor, double>(nameof(ImageHeight), 1, defaultBindingMode: BindingMode.TwoWay);

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PlacementXProperty || change.Property == PlacementYProperty ||
            change.Property == PlacementWidthProperty || change.Property == PlacementHeightProperty ||
            change.Property == ImageWidthProperty || change.Property == ImageHeightProperty)
            InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(new SolidColorBrush(Color.FromRgb(35, 43, 55)), new Pen(Brushes.SlateGray, 1), Bounds);
        var rectangle = DisplayRectangle();
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(45, 70, 150, 230)), new Pen(Brushes.DodgerBlue, 2), rectangle);
        context.DrawRectangle(Brushes.DodgerBlue, null, new Rect(rectangle.Right - 7, rectangle.Bottom - 7, 7, 7));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var point = e.GetPosition(this);
        var rectangle = DisplayRectangle();
        if (!rectangle.Inflate(8).Contains(point)) return;
        _resizing = Math.Abs(point.X - rectangle.Right) <= 14 && Math.Abs(point.Y - rectangle.Bottom) <= 14;
        _lastPoint = point;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_lastPoint is not Point previous) return;
        var point = e.GetPosition(this);
        var scaleX = ImageWidth / Math.Max(1, Bounds.Width);
        var scaleY = ImageHeight / Math.Max(1, Bounds.Height);
        var dx = (point.X - previous.X) * scaleX;
        var dy = (point.Y - previous.Y) * scaleY;
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
        _lastPoint = null;
        e.Pointer.Capture(null);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
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

    private Rect DisplayRectangle()
    {
        var imageWidth = Math.Max(1, ImageWidth);
        var imageHeight = Math.Max(1, ImageHeight);
        return new Rect(
            PlacementX / imageWidth * Bounds.Width,
            PlacementY / imageHeight * Bounds.Height,
            PlacementWidth / imageWidth * Bounds.Width,
            PlacementHeight / imageHeight * Bounds.Height);
    }
}
