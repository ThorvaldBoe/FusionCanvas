using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;

namespace FusionCanvas.App.StageTools;

/// <summary>Displays a PNG canvas and provides pointer and keyboard movement/resizing of its visible artwork.</summary>
public sealed class ArtworkAdjustmentSurface : Control
{
    public static readonly StyledProperty<Bitmap?> SourceProperty = AvaloniaProperty.Register<ArtworkAdjustmentSurface, Bitmap?>(nameof(Source));
    public static readonly StyledProperty<ArtworkPixelRectangle?> SourceBoundsProperty = AvaloniaProperty.Register<ArtworkAdjustmentSurface, ArtworkPixelRectangle?>(nameof(SourceBounds));
    public static readonly StyledProperty<ArtworkPixelRectangle?> DraftBoundsProperty = AvaloniaProperty.Register<ArtworkAdjustmentSurface, ArtworkPixelRectangle?>(nameof(DraftBounds));
    public static readonly StyledProperty<bool> IsAdjustmentModeProperty = AvaloniaProperty.Register<ArtworkAdjustmentSurface, bool>(nameof(IsAdjustmentMode));

    private const double HandleRadius = 7;
    private static readonly Pen ArtworkPen = new(Brushes.Red, 2);
    private static readonly Pen HandlePen = new(Brushes.White, 1.5);

    private PointerOperation _pointerOperation;
    private Point _pointerStart;
    private ArtworkPixelRectangle _boundsAtPointerStart;
    private bool _keyboardResizeMode;

    public ArtworkAdjustmentSurface()
    {
        Focusable = true;
        ClipToBounds = true;
        AutomationProperties.SetName(this, "Artwork adjustment surface. Use arrows to move; press Space to switch to proportional resize.");
    }

    public event EventHandler<ArtworkPixelRectangleEventArgs>? DraftBoundsChanged;

    public Bitmap? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public ArtworkPixelRectangle? SourceBounds { get => GetValue(SourceBoundsProperty); set => SetValue(SourceBoundsProperty, value); }
    public ArtworkPixelRectangle? DraftBounds { get => GetValue(DraftBoundsProperty); set => SetValue(DraftBoundsProperty, value); }
    public bool IsAdjustmentMode { get => GetValue(IsAdjustmentModeProperty); set => SetValue(IsAdjustmentModeProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty || change.Property == SourceBoundsProperty || change.Property == DraftBoundsProperty || change.Property == IsAdjustmentModeProperty)
            InvalidateVisual();
        if (change.Property == IsAdjustmentModeProperty)
        {
            _keyboardResizeMode = false;
            AutomationProperties.SetName(this, IsAdjustmentMode
                ? "Artwork adjustment rectangle. Use arrows to move; press Space to switch to proportional resize."
                : "Artwork preview surface");
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!IsAdjustmentMode || Source is null || SourceBounds is not ArtworkPixelRectangle sourceBounds || DraftBounds is not ArtworkPixelRectangle draftBounds
            || Source.PixelSize.Width <= 0 || Source.PixelSize.Height <= 0)
            return;

        var imageRect = GetImageRect();
        using (context.PushClip(imageRect))
        {
            var sourceRect = new Rect(sourceBounds.X, sourceBounds.Y, sourceBounds.Width, sourceBounds.Height);
            var targetRect = PixelRectToView(draftBounds, imageRect);
            context.DrawImage(Source, sourceRect, targetRect);
            context.DrawRectangle(null, ArtworkPen, targetRect);
            var handleCenter = new Point(targetRect.Right, targetRect.Bottom);
            context.DrawEllipse(Brushes.Red, HandlePen, handleCenter, HandleRadius, HandleRadius);
            if (_keyboardResizeMode)
                context.DrawEllipse(null, new Pen(Brushes.Yellow, 2), handleCenter, HandleRadius + 3, HandleRadius + 3);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsAdjustmentMode || DraftBounds is not ArtworkPixelRectangle bounds || SourceBounds is null
            || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var pixelPoint = ToPixelPoint(e.GetPosition(this));
        if (pixelPoint is null) return;
        var hitRadius = HandleRadius / GetImageScale();
        var handleX = bounds.X + bounds.Width;
        var handleY = bounds.Y + bounds.Height;
        if (Math.Abs(pixelPoint.Value.X - handleX) <= hitRadius && Math.Abs(pixelPoint.Value.Y - handleY) <= hitRadius)
            _pointerOperation = PointerOperation.Resize;
        else if (pixelPoint.Value.X >= bounds.X && pixelPoint.Value.X <= handleX && pixelPoint.Value.Y >= bounds.Y && pixelPoint.Value.Y <= handleY)
            _pointerOperation = PointerOperation.Move;
        else
            return;

        Focus();
        _pointerStart = pixelPoint.Value;
        _boundsAtPointerStart = bounds;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointerOperation == PointerOperation.None || DraftBounds is not ArtworkPixelRectangle)
            return;

        var current = ToPixelPoint(e.GetPosition(this));
        if (current is null) return;
        var dx = current.Value.X - _pointerStart.X;
        var dy = current.Value.Y - _pointerStart.Y;
        var updated = _pointerOperation == PointerOperation.Move
            ? _boundsAtPointerStart with { X = _boundsAtPointerStart.X + dx, Y = _boundsAtPointerStart.Y + dy }
            : ResizeProportionally(_boundsAtPointerStart, dx, dy);
        SetDraftBounds(updated);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointerOperation == PointerOperation.None) return;
        _pointerOperation = PointerOperation.None;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsAdjustmentMode || DraftBounds is not ArtworkPixelRectangle bounds)
            return;

        if (e.Key == Key.Space)
        {
            _keyboardResizeMode = !_keyboardResizeMode;
            AutomationProperties.SetName(this, _keyboardResizeMode
                ? "Artwork resize handle. Use arrows to resize proportionally; press Space to switch to movement."
                : "Artwork adjustment rectangle. Use arrows to move; press Space to switch to proportional resize.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10d : 1d;
        var (dx, dy) = e.Key switch
        {
            Key.Left => (-step, 0d),
            Key.Right => (step, 0d),
            Key.Up => (0d, -step),
            Key.Down => (0d, step),
            _ => (0d, 0d)
        };
        if (dx == 0 && dy == 0) return;

        var updated = _keyboardResizeMode
            ? ResizeProportionally(bounds, dx, dy)
            : bounds with { X = bounds.X + dx, Y = bounds.Y + dy };
        SetDraftBounds(updated);
        e.Handled = true;
    }

    private void SetDraftBounds(ArtworkPixelRectangle bounds)
    {
        DraftBounds = bounds;
        DraftBoundsChanged?.Invoke(this, new ArtworkPixelRectangleEventArgs(bounds));
    }

    private Rect GetImageRect()
    {
        if (Source is null || Bounds.Width <= 0 || Bounds.Height <= 0) return default;
        var scale = Math.Min(Bounds.Width / Source.PixelSize.Width, Bounds.Height / Source.PixelSize.Height);
        var width = Source.PixelSize.Width * scale;
        var height = Source.PixelSize.Height * scale;
        return new Rect((Bounds.Width - width) / 2, (Bounds.Height - height) / 2, width, height);
    }

    private double GetImageScale()
    {
        if (Source is null || Source.PixelSize.Width <= 0 || Source.PixelSize.Height <= 0) return 1;
        return Math.Min(Bounds.Width / Source.PixelSize.Width, Bounds.Height / Source.PixelSize.Height);
    }

    private Point? ToPixelPoint(Point point)
    {
        if (Source is null) return null;
        var imageRect = GetImageRect();
        var scale = GetImageScale();
        if (scale <= 0) return null;
        return new Point((point.X - imageRect.X) / scale, (point.Y - imageRect.Y) / scale);
    }

    private Rect PixelRectToView(ArtworkPixelRectangle bounds, Rect imageRect)
    {
        var scale = GetImageScale();
        return new Rect(imageRect.X + bounds.X * scale, imageRect.Y + bounds.Y * scale, bounds.Width * scale, bounds.Height * scale);
    }

    private static ArtworkPixelRectangle ResizeProportionally(ArtworkPixelRectangle bounds, double dx, double dy)
    {
        var denominator = bounds.Width * bounds.Width + bounds.Height * bounds.Height;
        if (denominator <= 0) return bounds;
        var factor = Math.Max(0.001, 1 + (dx * bounds.Width + dy * bounds.Height) / denominator);
        return bounds with { Width = bounds.Width * factor, Height = bounds.Height * factor };
    }

    private enum PointerOperation { None, Move, Resize }
}
