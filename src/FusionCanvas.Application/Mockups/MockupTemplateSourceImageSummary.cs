using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateSourceImageSummary(Guid Id, Guid SourceAssetId, string Name, string WorkspaceRelativePath, RasterImageInfo Dimensions, MockupImageSpaceMapping? ImageMapping, IReadOnlyList<Guid> OptionValueIds, string? PreviewPath = null)
{
    public bool HasCompleteMetadata => OptionValueIds.Count > 0 && ImageMapping is not null;
}
