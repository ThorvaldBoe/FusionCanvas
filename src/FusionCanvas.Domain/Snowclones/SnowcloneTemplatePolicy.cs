using System.Text;

namespace FusionCanvas.Domain.Snowclones;

public static class SnowcloneTemplatePolicy
{
    public static SnowcloneTemplateValidation Validate(string? phrase, string? guidance)
    {
        var normalizedPhrase = phrase?.Trim() ?? string.Empty;
        var normalizedGuidance = guidance?.Trim() ?? string.Empty;

        if (normalizedPhrase.Length == 0)
        {
            return SnowcloneTemplateValidation.Failure(normalizedPhrase, normalizedGuidance, "Phrase is required.");
        }

        if (normalizedPhrase.IndexOfAny(['\r', '\n']) >= 0)
        {
            return SnowcloneTemplateValidation.Failure(normalizedPhrase, normalizedGuidance, "Phrase must be a single line.");
        }

        var placeholderStart = -1;
        var placeholderCount = 0;

        for (var index = 0; index < normalizedPhrase.Length; index++)
        {
            var character = normalizedPhrase[index];
            if (character == '{')
            {
                if (placeholderStart >= 0)
                {
                    return SnowcloneTemplateValidation.Failure(normalizedPhrase, normalizedGuidance, "Placeholders cannot be nested.");
                }

                placeholderStart = index;
                continue;
            }

            if (character != '}')
            {
                continue;
            }

            if (placeholderStart < 0)
            {
                return SnowcloneTemplateValidation.Failure(normalizedPhrase, normalizedGuidance, "Phrase contains an unmatched closing brace.");
            }

            var content = normalizedPhrase[(placeholderStart + 1)..index];
            if (string.IsNullOrWhiteSpace(content))
            {
                return SnowcloneTemplateValidation.Failure(normalizedPhrase, normalizedGuidance, "Placeholder names cannot be empty.");
            }

            placeholderCount++;
            placeholderStart = -1;
        }

        if (placeholderStart >= 0)
        {
            return SnowcloneTemplateValidation.Failure(normalizedPhrase, normalizedGuidance, "Phrase contains an unmatched opening brace.");
        }

        if (placeholderCount == 0)
        {
            return SnowcloneTemplateValidation.Failure(normalizedPhrase, normalizedGuidance, "Phrase must contain at least one brace-delimited placeholder.");
        }

        if (normalizedGuidance.Length == 0)
        {
            return SnowcloneTemplateValidation.Failure(normalizedPhrase, normalizedGuidance, "Guidance is required.");
        }

        return SnowcloneTemplateValidation.Success(
            normalizedPhrase,
            normalizedGuidance,
            CreateDuplicateKey(normalizedPhrase));
    }

    public static string CreateDuplicateKey(string phrase)
    {
        ArgumentNullException.ThrowIfNull(phrase);

        var builder = new StringBuilder(phrase.Length);
        var pendingWhitespace = false;

        foreach (var character in phrase.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingWhitespace = builder.Length > 0;
                continue;
            }

            if (pendingWhitespace)
            {
                builder.Append(' ');
                pendingWhitespace = false;
            }

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }
}
