namespace FusionCanvas.Application.DesignFiles;

public sealed record GlobalColorRemovalParameters
{
    public GlobalColorRemovalParameters(GlobalColorRemovalColor color, double tolerance)
    {
        if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between zero and one.");
        }

        Color = color;
        Tolerance = tolerance;
    }

    public GlobalColorRemovalColor Color { get; }

    public double Tolerance { get; }
}
