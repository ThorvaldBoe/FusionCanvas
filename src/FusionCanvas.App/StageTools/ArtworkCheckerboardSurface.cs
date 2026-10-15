using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace FusionCanvas.App.StageTools;

public sealed class ArtworkCheckerboardSurface : Control
{
    public static readonly StyledProperty<Bitmap?> SourceProperty = AvaloniaProperty.Register<ArtworkCheckerboardSurface, Bitmap?>(nameof(Source));
    private static readonly IBrush CheckerLight = new SolidColorBrush(Color.FromRgb(235, 235, 235));
    private static readonly IBrush CheckerDark = new SolidColorBrush(Color.FromRgb(190, 190, 190));
    private const double CheckerCell = 16;

    public Bitmap? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty) InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Source is null || Source.PixelSize.Width <= 0 || Source.PixelSize.Height <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;
        var scale = Math.Min(Bounds.Width / Source.PixelSize.Width, Bounds.Height / Source.PixelSize.Height);
        var width = Source.PixelSize.Width * scale;
        var height = Source.PixelSize.Height * scale;
        var imageRect = new Rect((Bounds.Width - width) / 2, (Bounds.Height - height) / 2, width, height);
        context.FillRectangle(CheckerLight, imageRect);
        var startX = Math.Floor(imageRect.X / CheckerCell) * CheckerCell;
        var startY = Math.Floor(imageRect.Y / CheckerCell) * CheckerCell;
        for (var y = startY; y < imageRect.Bottom; y += CheckerCell)
        for (var x = startX; x < imageRect.Right; x += CheckerCell)
        {
            var column = (int)Math.Floor(x / CheckerCell);
            var row = (int)Math.Floor(y / CheckerCell);
            if (((column + row) & 1) == 0)
                context.FillRectangle(CheckerDark, new Rect(x, y, CheckerCell, CheckerCell).Intersect(imageRect));
        }
    }
}
