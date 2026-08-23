namespace FusionCanvas.Domain.Catalog;

public readonly record struct DesignAreaPhysicalSize(double WidthInches, double HeightInches)
{
    private const double MillimetresPerInch = 25.4;

    public double WidthMillimetres => WidthInches * MillimetresPerInch;
    public double HeightMillimetres => HeightInches * MillimetresPerInch;
}
