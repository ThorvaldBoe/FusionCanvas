using System.Windows.Input;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Stores;

public sealed record OfferingChoiceGroupViewModel(
    OfferingOption Option,
    IReadOnlyList<OfferingOptionValue> Values,
    ICommand ArchiveOptionCommand)
{
    public string Name => Option.Name;
    public string KindLabel => Option.OptionKind.ToString();
    public bool IsColor => Option.OptionKind == OptionKind.Color;
    public double CardWidth => IsColor ? 390 : 235;
    public string ValuesSummary => Values.Count == 0 ? "No values configured" : string.Join("   ", Values.Select(value => value.Value));
    public string AccessibleOverflowName => $"More actions for {Option.Name}";
    public string OverflowAutomationId => $"Catalog.OptionOverflow.{Option.Id}";
}
