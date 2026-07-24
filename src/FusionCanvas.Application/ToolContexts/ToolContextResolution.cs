namespace FusionCanvas.Application.ToolContexts;

public sealed record ToolContextResolution(
    bool IsAvailable,
    string? UnavailableReason,
    ToolContextScopeSummary ScopeSummary,
    ToolContext? Context)
{
    public static ToolContextResolution Unavailable(string reason) =>
        new(
            false,
            string.IsNullOrWhiteSpace(reason) ? "Tool context is unavailable." : reason,
            new ToolContextScopeSummary(ToolContextScopeKind.Unavailable, "Unavailable", reason, false),
            null);
}
