using FusionCanvas.UiDescription.Diagnostics;
using FusionCanvas.UiDescription.Model;

namespace FusionCanvas.UiDescription.Validation;

public sealed class UiStateProjector
{
    public UiValidationResult Project(UiDescriptionDocument document, string stateName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);

        if (!document.States.TryGetValue(stateName, out var state))
        {
            var diagnostic = new UiDiagnostic(
                "UIDL130",
                UiDiagnosticSeverity.Error,
                document.Source,
                $"Unknown state '{stateName}'.",
                stateName);
            return new UiValidationResult(null, [diagnostic]);
        }

        var overrides = state.Overrides.ToDictionary(item => item.Target, StringComparer.Ordinal);
        var projectedRoot = ProjectComponent(document.Screen.Root, overrides);
        var projectedScreen = document.Screen with { Root = projectedRoot };
        return new UiValidationResult(document with { Screen = projectedScreen }, []);
    }

    private static UiComponent ProjectComponent(
        UiComponent component,
        IReadOnlyDictionary<string, UiStateOverride> overrides)
    {
        var children = component.Children
            .Select(child => ProjectComponent(child, overrides))
            .ToArray();

        if (!overrides.TryGetValue(component.Id, out var item))
        {
            return component with { Children = children };
        }

        return component with
        {
            Children = children,
            Visible = item.Visible ?? component.Visible,
            Enabled = item.Enabled ?? component.Enabled,
            Text = item.Text ?? component.Text,
            Items = item.Items ?? component.Items,
            TableRows = item.TableRows ?? component.TableRows
        };
    }
}
