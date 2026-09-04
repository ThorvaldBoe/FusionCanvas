namespace FusionCanvas.Application.Mockups;
public sealed record MockupGenerationOutput(Guid AssetId, string Name, string WorkspaceRelativePath, string ColorValue, Guid TemplateId, int TemplateRevision, Guid DesignAssetId);
