using System.Windows.Input;
using AvaloniaVirtualDataGrid.Core;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Stores;

public sealed class OfferingChoiceGroupViewModel
{
    public OfferingChoiceGroupViewModel(
        OfferingOption option,
        IReadOnlyList<OfferingOptionValue> values,
        ICommand archiveOptionCommand)
    {
        Option = option ?? throw new ArgumentNullException(nameof(option));
        ArgumentNullException.ThrowIfNull(values);
        ArchiveOptionCommand = archiveOptionCommand ?? throw new ArgumentNullException(nameof(archiveOptionCommand));
        Values = values
            .Where(value => !value.IsArchived)
            .OrderBy(value => value.SortOrder)
            .ThenBy(value => value.Id)
            .ToArray();
        if (Option.OptionKind != OptionKind.Color)
        {
            ValueRows.Reset(Values.Select((value, index) => new AvailableOptionValueGridRowViewModel(value.Value, index % 2 == 1)));
        }
    }

    public OfferingOption Option { get; }

    public IReadOnlyList<OfferingOptionValue> Values { get; }

    public ICommand ArchiveOptionCommand { get; }

    public InMemoryDataProvider<AvailableOptionValueGridRowViewModel> ValueRows { get; } = new();

    public string Name => Option.Name;
    public string KindLabel => Option.OptionKind.ToString();
    public bool IsColor => Option.OptionKind == OptionKind.Color;
    public bool HasValues => Values.Count > 0;
    public bool ShowValueGrid => !IsColor && HasValues;
    public bool ShowEmptyValueSummary => !IsColor && !HasValues;
    public double CardWidth => IsColor ? 390 : 235;
    public double ValueGridHeight => 120;
    public double ValueGridRowHeight => 32;
    public string ValueGridAccessibleName => $"Available {KindLabel} values for {Name}";
    public string ValueGridAutomationId => $"Catalog.OptionValuesGrid.{Option.Id:N}";
    public string EmptyValueSummaryAutomationId => $"Catalog.OptionValuesEmpty.{Option.Id:N}";
    public string ValuesSummary => Values.Count == 0 ? "No values configured" : string.Join("   ", Values.Select(value => value.Value));
    public string AccessibleOverflowName => $"More actions for {Option.Name}";
    public string OverflowAutomationId => $"Catalog.OptionOverflow.{Option.Id}";
}

public sealed record AvailableOptionValueGridRowViewModel(string Value, bool IsAlternate)
{
    public string AccessibleName => Value;
    public string AccessibleHelpText => $"Configured option value: {Value}";
}
