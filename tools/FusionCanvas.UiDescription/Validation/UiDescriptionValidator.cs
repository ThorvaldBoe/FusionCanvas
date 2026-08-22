using System.Text.RegularExpressions;
using FusionCanvas.UiDescription.Diagnostics;
using FusionCanvas.UiDescription.Model;

namespace FusionCanvas.UiDescription.Validation;

public sealed partial class UiDescriptionValidator
{
    public UiValidationResult Validate(UiDescriptionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var diagnostics = new List<UiDiagnostic>();

        if (document.SchemaVersion != 1)
        {
            Add(diagnostics, "UIDL100", document.Source, $"Unsupported schema version '{document.SchemaVersion}'.", "schemaVersion");
        }

        if (!string.Equals(document.TokenProfile, UiVocabulary.TokenProfile, StringComparison.Ordinal))
        {
            Add(diagnostics, "UIDL101", document.Source, $"Unsupported token profile '{document.TokenProfile}'.", "tokenProfile");
        }

        if (document.Screen.ViewportWidth <= 0 || document.Screen.ViewportHeight <= 0)
        {
            Add(diagnostics, "UIDL102", document.Screen.Source, "Viewport width and height must be positive.", document.Screen.Id);
        }

        if (!UiVocabulary.IsContainer(document.Screen.Root.Kind))
        {
            Add(diagnostics, "UIDL103", document.Screen.Root.Source, "The root component must be a container.", document.Screen.Root.Id);
        }

        var components = new Dictionary<string, UiComponent>(StringComparer.Ordinal);
        ValidateComponent(document.Screen.Root, null, components, diagnostics);
        ValidateStates(document, components, diagnostics);
        diagnostics.Sort();
        return diagnostics.Count == 0
            ? new UiValidationResult(document, [])
            : new UiValidationResult(null, diagnostics);
    }

    private static void ValidateComponent(
        UiComponent component,
        UiComponent? parent,
        Dictionary<string, UiComponent> components,
        List<UiDiagnostic> diagnostics)
    {
        ValidateIdentity(component, components, diagnostics);
        ValidateKindAndVariant(component, diagnostics);
        ValidateSizing(component, diagnostics);
        ValidateLayoutProperties(component, parent, diagnostics);
        ValidateContentProperties(component, diagnostics);

        if (UiVocabulary.IsLeaf(component.Kind) && component.Children.Count > 0)
        {
            Add(diagnostics, "UIDL110", component.Source, "Leaf components cannot contain children.", component.Id);
        }

        foreach (var child in component.Children)
        {
            ValidateComponent(child, component, components, diagnostics);
        }
    }

    private static void ValidateIdentity(
        UiComponent component,
        Dictionary<string, UiComponent> components,
        List<UiDiagnostic> diagnostics)
    {
        if (!IdentifierRegex().IsMatch(component.Id))
        {
            Add(diagnostics, "UIDL104", component.Source, "Component IDs must use lowercase kebab-case.", component.Id);
        }

        if (components.TryGetValue(component.Id, out var first))
        {
            Add(
                diagnostics,
                "UIDL105",
                component.Source,
                $"Duplicate component ID; first declared at {first.Source.Line}:{first.Source.Column}.",
                component.Id);
        }
        else
        {
            components.Add(component.Id, component);
        }
    }

    private static void ValidateKindAndVariant(UiComponent component, List<UiDiagnostic> diagnostics)
    {
        if (!UiVocabulary.IsContainer(component.Kind) && !UiVocabulary.IsLeaf(component.Kind))
        {
            Add(diagnostics, "UIDL106", component.Source, $"Unknown component kind '{component.Kind}'.", component.Id);
            return;
        }

        var variant = component.Variant ?? "default";
        if (!UiVocabulary.Variants[component.Kind].Contains(variant))
        {
            Add(diagnostics, "UIDL107", component.Source, $"Variant '{variant}' is not valid for '{component.Kind}'.", component.Id);
        }
    }

    private static void ValidateSizing(UiComponent component, List<UiDiagnostic> diagnostics)
    {
        ValidateLength(component.Width, "width", component, diagnostics);
        ValidateLength(component.Height, "height", component, diagnostics);

        if (component.MinWidth < 0 || component.MinHeight < 0)
        {
            Add(diagnostics, "UIDL108", component.Source, "Minimum sizes cannot be negative.", component.Id);
        }

        if (component.Width is { Kind: UiLengthKind.Fixed } width && component.MinWidth > width.Value)
        {
            Add(diagnostics, "UIDL108", component.Source, "minWidth cannot exceed a fixed width.", component.Id);
        }

        if (component.Height is { Kind: UiLengthKind.Fixed } height && component.MinHeight > height.Value)
        {
            Add(diagnostics, "UIDL108", component.Source, "minHeight cannot exceed a fixed height.", component.Id);
        }
    }

    private static void ValidateLength(
        UiLength? length,
        string property,
        UiComponent component,
        List<UiDiagnostic> diagnostics)
    {
        if (length is { Kind: UiLengthKind.Fixed, Value: < 0 })
        {
            Add(diagnostics, "UIDL108", component.Source, $"Fixed {property} cannot be negative.", component.Id);
        }
    }

    private static void ValidateLayoutProperties(
        UiComponent component,
        UiComponent? parent,
        List<UiDiagnostic> diagnostics)
    {
        if (component.Gap is not null && !UiVocabulary.SpacingTokens.Contains(component.Gap))
        {
            Add(diagnostics, "UIDL109", component.Source, $"Unknown gap token '{component.Gap}'.", component.Id);
        }

        if (component.Padding is not null && !UiVocabulary.SpacingTokens.Contains(component.Padding))
        {
            Add(diagnostics, "UIDL109", component.Source, $"Unknown padding token '{component.Padding}'.", component.Id);
        }

        if (component.Align is not null && !UiVocabulary.Alignments.Contains(component.Align))
        {
            Add(diagnostics, "UIDL109", component.Source, $"Unknown alignment '{component.Align}'.", component.Id);
        }

        if (component.Axis is not null && component.Axis is not ("vertical" or "horizontal"))
        {
            Add(diagnostics, "UIDL109", component.Source, $"Unknown axis '{component.Axis}'.", component.Id);
        }

        if (component.Axis is not null && component.Kind != "stack")
        {
            Add(diagnostics, "UIDL111", component.Source, "Only stack components can declare an axis.", component.Id);
        }

        if ((component.Columns.Count > 0 || component.RowTracks.Count > 0) && component.Kind != "grid")
        {
            Add(diagnostics, "UIDL111", component.Source, "Only grid components can declare tracks.", component.Id);
        }

        foreach (var track in component.Columns.Concat(component.RowTracks))
        {
            if (track is { Kind: UiLengthKind.Fixed, Value: < 0 })
            {
                Add(diagnostics, "UIDL108", component.Source, "Grid track sizes cannot be negative.", component.Id);
            }
        }

        if (component.Kind == "grid" && component.Columns.Count == 0)
        {
            Add(diagnostics, "UIDL112", component.Source, "A grid requires at least one column track.", component.Id);
        }

        if (component.ColumnSpan <= 0 || component.RowSpan <= 0)
        {
            Add(diagnostics, "UIDL112", component.Source, "Grid spans must be positive.", component.Id);
        }

        if (parent?.Kind == "grid")
        {
            if (component.Column is null || component.Row is null)
            {
                Add(diagnostics, "UIDL112", component.Source, "Grid children require explicit row and column placement.", component.Id);
            }
            else if (component.Column < 0 || component.Column + component.ColumnSpan > parent.Columns.Count || component.Row < 0)
            {
                Add(diagnostics, "UIDL112", component.Source, "Grid placement falls outside the declared tracks.", component.Id);
            }
        }
        else if (component.Column is not null || component.Row is not null || component.ColumnSpan != 1 || component.RowSpan != 1)
        {
            Add(diagnostics, "UIDL112", component.Source, "Grid placement is only valid for children of a grid.", component.Id);
        }
    }

    private static void ValidateContentProperties(UiComponent component, List<UiDiagnostic> diagnostics)
    {
        if (component.Items.Count > 0 && component.Kind is not ("list" or "select"))
        {
            Add(diagnostics, "UIDL113", component.Source, "Only list and select components can declare items.", component.Id);
        }

        if ((component.TableColumns.Count > 0 || component.TableRows.Count > 0) && component.Kind != "table")
        {
            Add(diagnostics, "UIDL113", component.Source, "Only table components can declare table data.", component.Id);
        }

        if (component.Kind == "table")
        {
            if (component.TableColumns.Count == 0)
            {
                Add(diagnostics, "UIDL114", component.Source, "A table requires at least one column.", component.Id);
            }

            foreach (var row in component.TableRows.Where(row => row.Count != component.TableColumns.Count))
            {
                Add(diagnostics, "UIDL114", component.Source, $"Table row has {row.Count} cells but {component.TableColumns.Count} columns are declared.", component.Id);
            }
        }
    }

    private static void ValidateStates(
        UiDescriptionDocument document,
        IReadOnlyDictionary<string, UiComponent> components,
        List<UiDiagnostic> diagnostics)
    {
        foreach (var state in document.States.Values.OrderBy(state => state.Source.Line).ThenBy(state => state.Source.Column))
        {
            if (!IdentifierRegex().IsMatch(state.Name))
            {
                Add(diagnostics, "UIDL120", state.Source, "State names must use lowercase kebab-case.", state.Name);
            }

            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in state.Overrides)
            {
                if (!targets.Add(item.Target))
                {
                    Add(diagnostics, "UIDL121", item.Source, "A state cannot override the same target more than once.", item.Target);
                }

                if (!components.TryGetValue(item.Target, out var target))
                {
                    Add(diagnostics, "UIDL122", item.Source, "State override targets an unknown component.", item.Target);
                    continue;
                }

                if (item.Visible is null && item.Enabled is null && item.Text is null && item.Items is null && item.TableRows is null)
                {
                    Add(diagnostics, "UIDL123", item.Source, "A state override must change at least one supported property.", item.Target);
                }

                if (item.Enabled is not null && !UiVocabulary.IsInteractive(target.Kind))
                {
                    Add(diagnostics, "UIDL123", item.Source, $"'{target.Kind}' does not support enabled overrides.", item.Target);
                }

                if (item.Text is not null && !UiVocabulary.SupportsText(target.Kind))
                {
                    Add(diagnostics, "UIDL123", item.Source, $"'{target.Kind}' does not support text overrides.", item.Target);
                }

                if (item.Items is not null && target.Kind is not ("list" or "select"))
                {
                    Add(diagnostics, "UIDL123", item.Source, $"'{target.Kind}' does not support item overrides.", item.Target);
                }

                if (item.TableRows is not null)
                {
                    if (target.Kind != "table")
                    {
                        Add(diagnostics, "UIDL123", item.Source, $"'{target.Kind}' does not support table row overrides.", item.Target);
                    }
                    else if (item.TableRows.Any(row => row.Count != target.TableColumns.Count))
                    {
                        Add(diagnostics, "UIDL124", item.Source, "An overridden table row does not match the declared column count.", item.Target);
                    }
                }
            }
        }
    }

    private static void Add(
        ICollection<UiDiagnostic> diagnostics,
        string code,
        UiSourceLocation location,
        string message,
        string? subject = null) =>
        diagnostics.Add(new UiDiagnostic(code, UiDiagnosticSeverity.Error, location, message, subject));

    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
