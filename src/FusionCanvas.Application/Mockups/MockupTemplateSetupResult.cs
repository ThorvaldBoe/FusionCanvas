using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateSetupResult(bool Succeeded, string? Error, MockupTemplateSetupState State, WorkspaceSnapshot? Snapshot = null, Guid? TemplateId = null)
{
    public static MockupTemplateSetupResult Success(MockupTemplateSetupState state, Guid? templateId = null) => new(true, null, state, TemplateId: templateId);
    public static MockupTemplateSetupResult Failure(string error, MockupTemplateSetupState state) => new(false, error, state);
}
