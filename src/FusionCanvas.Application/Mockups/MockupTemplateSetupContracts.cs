using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateReadinessSummary(Guid TemplateId, MockupTemplateLifecycle Lifecycle, IReadOnlyList<MockupTemplateReadinessBlocker> Blockers);
public sealed record EligibleMockupTemplateResult(bool Succeeded, string? Error, IReadOnlyList<MockupTemplate> Templates, IReadOnlyList<MockupTemplateReadinessBlocker> Blockers);
public sealed record MockupTemplateSetupState(Guid StoreId, bool IsReadOnly, IReadOnlyList<MockupTemplate> Templates, IReadOnlyList<MockupTemplateColorVariant> Colors, IReadOnlyList<MockupTemplateRevision> Revisions, IReadOnlyList<MockupTemplateReadinessSummary>? Readiness = null);
public sealed record MockupTemplateSetupResult(bool Succeeded, string? Error, MockupTemplateSetupState State, WorkspaceSnapshot? Snapshot = null, Guid? TemplateId = null)
{
    public static MockupTemplateSetupResult Success(MockupTemplateSetupState state, Guid? templateId = null) => new(true, null, state, TemplateId: templateId);
    public static MockupTemplateSetupResult Failure(string error, MockupTemplateSetupState state) => new(false, error, state);
}

public sealed record CreateMockupTemplateRequest(Guid StoreId, Guid OfferingId, string Name, Guid? TargetPlaceholderId = null, string? Description = null, string? PositionKey = null, string? ProviderMockupReference = null, MockupImageSpaceMapping? ImageMapping = null, IReadOnlyList<Guid>? ColorOptionValueIds = null);
public sealed record DuplicateMockupTemplateRequest(Guid StoreId, Guid TemplateId);
public sealed record AddMockupTemplateColorRequest(Guid StoreId, Guid TemplateId, Guid ColorOptionValueId);
public sealed record ArchiveMockupTemplateColorRequest(Guid StoreId, Guid TemplateColorId);
public sealed record ArchiveMockupTemplateRequest(Guid StoreId, Guid TemplateId);
public sealed record UpdateMockupTemplateRequest(Guid StoreId, Guid TemplateId, string? Name = null, string? Description = null, Guid? TargetPlaceholderId = null, string? PositionKey = null, bool ReplaceProviderImage = false, string? ProviderMockupReference = null, MockupImageSpaceMapping? ImageMapping = null, IReadOnlyList<Guid>? ReplaceColorOptionValueIds = null, bool ReplaceTargetPlaceholder = false);

public interface IMockupTemplateSetupService
{
    Task<MockupTemplateSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> CreateTemplateAsync(CreateMockupTemplateRequest request, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> DuplicateTemplateAsync(DuplicateMockupTemplateRequest request, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> AddColorAsync(AddMockupTemplateColorRequest request, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> ArchiveColorAsync(ArchiveMockupTemplateColorRequest request, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> ArchiveTemplateAsync(ArchiveMockupTemplateRequest request, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> RestoreTemplateAsync(ArchiveMockupTemplateRequest request, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> UpdateTemplateAsync(UpdateMockupTemplateRequest request, CancellationToken cancellationToken = default);
    Task<EligibleMockupTemplateResult> GetEligibleTemplatesAsync(Guid storeId, Guid offeringId, Guid? requestedTemplateId = null, CancellationToken cancellationToken = default);
}
