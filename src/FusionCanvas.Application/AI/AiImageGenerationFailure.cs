namespace FusionCanvas.Application.AI;

public sealed record AiImageGenerationFailure(AiImageGenerationFailureKind Kind, string Message, bool WasDispatched = false);
