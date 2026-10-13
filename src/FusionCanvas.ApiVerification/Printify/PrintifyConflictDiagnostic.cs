using FusionCanvas.Application.Listings;

namespace FusionCanvas.ApiVerification.Printify;

internal static class PrintifyConflictDiagnostic
{
    public static string Format(ListingLifecycleConflict conflict)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        if (conflict.Comparison.Changes.Count == 0)
            return "No changed fields were supplied by the lifecycle conflict.";

        return string.Join(", ", conflict.Comparison.Changes
            .OrderBy(change => change.Field, StringComparer.Ordinal)
            .Select(change => $"{change.Field} (remote changed: {YesNo(change.RemoteChanged)}; local changed: {YesNo(change.LocalChanged)})"));
    }

    private static string YesNo(bool value) => value ? "yes" : "no";
}
