using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Items;

public sealed record ItemStageSavePayload(
    WorkflowStage Stage,
    string? Idea,
    string? ConceptIdea,
    string? Phrase,
    string? GraphicDirection);
