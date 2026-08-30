using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateSourceImageSummary(Guid Id, Guid SourceAssetId, string Name, string WorkspaceRelativePath, RasterImageInfo Dimensions, MockupImageSpaceMapping ImageMapping, IReadOnlyList<Guid> OptionValueIds);
public sealed record MockupTemplateSourceReadiness(Guid VariantId, MockupTemplateSourceResolutionKind Kind, IReadOnlyList<Guid> SourceImageIds);
public sealed record MockupTemplateSourceState(IReadOnlyList<MockupTemplateSourceImageSummary> Images, IReadOnlyList<MockupTemplateSourceReadiness> Readiness, bool IsReady, string? Error = null);
public sealed record AddLocalMockupTemplateSourceRequest(Guid StoreId, Guid TemplateId, string SourcePath, IReadOnlyList<Guid> OptionValueIds, MockupImageSpaceMapping? ImageMapping = null);

public interface IMockupTemplateSourceImageService
{
    Task<MockupTemplateSourceState> LoadAsync(Guid storeId, Guid templateId, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> AddAsync(AddLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default);
}
