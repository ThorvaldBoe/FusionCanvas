using FusionCanvas.Domain.Text;

namespace FusionCanvas.Domain.Ideation;

public static class RejectionPhraseComparison
{
    public static string NormalizeKey(string phrase) => PhraseKeyNormalizer.Normalize(phrase);

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
