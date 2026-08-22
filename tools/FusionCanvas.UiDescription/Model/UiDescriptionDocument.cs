namespace FusionCanvas.UiDescription.Model;

public sealed record UiDescriptionDocument(
    int SchemaVersion,
    string TokenProfile,
    UiScreen Screen,
    IReadOnlyDictionary<string, UiState> States,
    UiSourceLocation Source);

public sealed record UiScreen(
    string Id,
    string Title,
    decimal ViewportWidth,
    decimal ViewportHeight,
    UiComponent Root,
    UiSourceLocation Source);

public sealed record UiState(
    string Name,
    IReadOnlyList<UiStateOverride> Overrides,
    UiSourceLocation Source);

public sealed record UiStateOverride(
    string Target,
    bool? Visible,
    bool? Enabled,
    string? Text,
    IReadOnlyList<string>? Items,
    IReadOnlyList<IReadOnlyList<string>>? TableRows,
    UiSourceLocation Source);
