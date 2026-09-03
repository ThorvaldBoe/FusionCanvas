using FusionCanvas.Application.Items;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationScopeResult(bool IsAvailable, IdeationScope? Scope, string? Error)
{
    public static IdeationScopeResult Available(IdeationScope scope) => new(true, scope, null);

    public static IdeationScopeResult Unavailable(string error) => new(false, null, error);
}
