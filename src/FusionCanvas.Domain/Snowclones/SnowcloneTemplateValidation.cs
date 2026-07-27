namespace FusionCanvas.Domain.Snowclones;

public sealed record SnowcloneTemplateValidation(
    bool IsValid,
    string Phrase,
    string Guidance,
    string DuplicateKey,
    IReadOnlyList<string> PlaceholderTokens,
    string? Error)
{
    public static SnowcloneTemplateValidation Success(
        string phrase,
        string guidance,
        string duplicateKey,
        IReadOnlyList<string> placeholderTokens) =>
        new(true, phrase, guidance, duplicateKey, placeholderTokens, null);

    public static SnowcloneTemplateValidation Failure(string phrase, string guidance, string error) =>
        new(false, phrase, guidance, string.Empty, [], error);
}
