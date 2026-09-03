using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record DuplicateMockupTemplateRequest(Guid StoreId, Guid TemplateId);
