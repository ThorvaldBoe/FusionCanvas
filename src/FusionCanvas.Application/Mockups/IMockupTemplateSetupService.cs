using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

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
