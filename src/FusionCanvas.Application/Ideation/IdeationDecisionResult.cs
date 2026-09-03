using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationDecisionResult(
    bool Succeeded,
    string? Error,
    WorkspaceSnapshot State,
    Item? CreatedItem = null,
    IdeationRejection? Rejection = null);
