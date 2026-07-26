namespace FusionCanvas.Application.Items;

public sealed record ItemContext(
    string? Description = null,
    string? Notes = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyList<Guid>? TagIds = null);
