namespace FusionCanvas.Application.DesignFiles;

/// <summary>A floating-point rectangle in source-image pixel coordinates.</summary>
public readonly record struct ArtworkPixelRectangle(double X, double Y, double Width, double Height)
{
    public bool IsFinitePositive =>
        double.IsFinite(X) && double.IsFinite(Y)
        && double.IsFinite(Width) && double.IsFinite(Height)
        && Width > 0 && Height > 0;

    public static ArtworkPixelRectangle FromIntegerBounds(int x, int y, int width, int height) =>
        new(x, y, width, height);
}
