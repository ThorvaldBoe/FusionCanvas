using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public interface IMockupTemplateSourceImageService
{
    Task<MockupTemplateSourceState> LoadAsync(Guid storeId, Guid templateId, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> AddAsync(AddLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default);
    Task<MockupTemplateSetupResult> UpdateAsync(UpdateLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default);
}
