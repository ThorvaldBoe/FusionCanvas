using System.Text;

namespace FusionCanvas.Domain.Ideation;

public static class RejectionPhraseComparison
{
    public static string NormalizeKey(string phrase)
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

    public static bool IsWithinScopeDuplicate(IdeationRejection first, IdeationRejection second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.Id == second.Id)
        {
            return false;
        }

        if (first.StoreId != second.StoreId || first.NicheId != second.NicheId)
        {
            return false;
        }

        if (first.GroupId != second.GroupId)
        {
            return false;
        }

        return NormalizeKey(first.Text).Equals(NormalizeKey(second.Text), StringComparison.Ordinal);
    }
}
