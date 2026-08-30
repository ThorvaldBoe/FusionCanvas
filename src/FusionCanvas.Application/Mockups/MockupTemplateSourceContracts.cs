using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateSourceImageSummary(Guid Id, Guid SourceAssetId, string Name, string WorkspaceRelativePath, RasterImageInfo Dimensions, MockupImageSpaceMapping? ImageMapping, IReadOnlyList<Guid> OptionValueIds, string? PreviewPath = null)
{
    public bool HasCompleteMetadata => OptionValueIds.Count > 0 && ImageMapping is not null;
}
public sealed record MockupTemplateSourceReadiness(Guid VariantId, MockupTemplateSourceResolutionKind Kind, IReadOnlyList<Guid> SourceImageIds);
public sealed record MockupTemplateSourceState(IReadOnlyList<MockupTemplateSourceImageSummary> Images, IReadOnlyList<MockupTemplateSourceReadiness> Readiness, bool IsReady, string? Error = null);
public sealed record AddLocalMockupTemplateSourceRequest(Guid StoreId, Guid TemplateId, string SourcePath, IReadOnlyList<Guid> OptionValueIds, MockupImageSpaceMapping? ImageMapping = null);
public sealed record UpdateLocalMockupTemplateSourceRequest(Guid StoreId, Guid TemplateId, Guid SourceImageId, IReadOnlyList<Guid> OptionValueIds, MockupImageSpaceMapping? ImageMapping = null, bool Archive = false);

public interface IMockupTemplateSourceImageService
{
    Task<MockupTemplateSourceState> LoadAsync(Guid storeId, Guid templateId, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> AddAsync(AddLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> UpdateAsync(UpdateLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default);
}
