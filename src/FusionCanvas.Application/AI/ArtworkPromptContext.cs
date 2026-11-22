namespace FusionCanvas.Application.AI;

public sealed record ArtworkPromptContext(
    string OriginalIdea,
    string ConceptIdea,
    string Phrase,
    string GraphicDirection,
    string TargetName,
    string Placement,
    AiImageSize TargetSize,
    string DecorationMethod,
    string? ArtworkGuidance = null,
    string? CreativeContext = null,
    string? Sll = null,
    bool SllIsStale = false,
    string? NicheContext = null,
    ArtworkContrast Contrast = ArtworkContrast.DarkOnLight,
    string? CompositionGuidance = null,
    string? ArtworkStyleTitle = null,
    string? ArtworkStyleDescription = null);
