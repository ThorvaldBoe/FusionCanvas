namespace FusionCanvas.App.StageTools;

public sealed record ArtworkStyleOption(Guid? Id, string Title, string Description, int ReferenceCount, int MissingReferenceCount)
{
    public static ArtworkStyleOption General { get; } = new(null, "General", "Use the default visual direction.", 0, 0);
}
