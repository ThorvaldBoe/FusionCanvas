using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Stores;

public sealed record OfferingChoiceGroupViewModel(OfferingOption Option, IReadOnlyList<OfferingOptionValue> Values)
{
    public string Name => Option.Name;
    public string KindLabel => Option.OptionKind.ToString();
    public string ValuesSummary => Values.Count == 0 ? "No values configured" : string.Join("   ", Values.Select(value => value.Value));
}
