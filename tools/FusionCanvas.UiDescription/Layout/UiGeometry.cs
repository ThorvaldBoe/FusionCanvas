namespace FusionCanvas.UiDescription.Layout;

public readonly record struct UiSize(decimal Width, decimal Height);

public readonly record struct UiRect(decimal X, decimal Y, decimal Width, decimal Height)
{
    public decimal Right => X + Width;

    public decimal Bottom => Y + Height;

    public UiRect Deflate(decimal amount) =>
        new(X + amount, Y + amount, Math.Max(0, Width - (2 * amount)), Math.Max(0, Height - (2 * amount)));
}
