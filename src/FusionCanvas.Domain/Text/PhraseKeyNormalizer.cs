using System.Text;

namespace FusionCanvas.Domain.Text;

public static class PhraseKeyNormalizer
{
    public static string Normalize(string phrase)
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
