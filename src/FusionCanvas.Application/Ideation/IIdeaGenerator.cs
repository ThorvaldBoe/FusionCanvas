using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public interface IIdeaGenerator
{
    Task<IdeaGenerationResult> GenerateAsync(
        IdeationGenerationContext context,
        int requestIndex,
        CancellationToken cancellationToken = default);
}
