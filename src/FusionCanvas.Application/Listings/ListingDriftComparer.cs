namespace FusionCanvas.Application.Listings;

public static class ListingDriftComparer
{
    public static ListingDriftComparison Compare(ListingSnapshot last, ListingSnapshot remote, ListingSnapshot local)
    {
        ArgumentNullException.ThrowIfNull(last);
        ArgumentNullException.ThrowIfNull(remote);
        ArgumentNullException.ThrowIfNull(local);
        var fields = last.Fields.Keys
            .Concat(remote.Fields.Keys)
            .Concat(local.Fields.Keys)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        var changes = new List<ListingDriftChange>();
        foreach (var field in fields)
        {
            last.Fields.TryGetValue(field, out var lastValue);
            remote.Fields.TryGetValue(field, out var remoteValue);
            local.Fields.TryGetValue(field, out var localValue);
            var remoteChanged = !string.Equals(lastValue, remoteValue, StringComparison.Ordinal);
            var localChanged = !string.Equals(lastValue, localValue, StringComparison.Ordinal);
            if (remoteChanged || localChanged)
                changes.Add(new(field, lastValue, remoteValue, localValue, remoteChanged, localChanged));
        }
        return new(changes);
    }
}
