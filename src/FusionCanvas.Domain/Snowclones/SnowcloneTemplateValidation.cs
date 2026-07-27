namespace FusionCanvas.Domain.Snowclones;

public sealed record SnowcloneTemplateValidation(
    bool IsValid,
    string Phrase,
    string Guidance,
    string DuplicateKey,
    string? Error)
{
    public static SnowcloneTemplateValidation Success(string phrase, string guidance, string duplicateKey) =>
        new(true, phrase, guidance, duplicateKey, null);

    public static SnowcloneTemplateValidation Failure(string phrase, string guidance, string error) =>
        new(false, phrase, guidance, string.Empty, error);
}
