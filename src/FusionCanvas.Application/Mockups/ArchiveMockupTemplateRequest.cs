using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record ArchiveMockupTemplateRequest(Guid StoreId, Guid TemplateId);
