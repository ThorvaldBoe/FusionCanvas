using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Headless;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Tests;

namespace FusionCanvas.App.Tests.TestSupport.Drivers;

internal sealed class StoreEditorDriver(StoreEditorWindow window)
{
    internal ComboBox PrintifyShop => Find<ComboBox>("StoreEditor.PrintifyShop");

    internal Button SaveStore => Find<Button>("StoreEditor.SaveStore");

    internal Button SaveBlueprint => Find<Button>("Catalog.SaveBlueprint");

    internal TextBox BlueprintName => Find<TextBox>("Catalog.BlueprintName");

    internal void SelectFirstPrintifyShop()
        => SelectPrintifyShop(0);

    internal void SelectPrintifyShop(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        var combo = PrintifyShop;
        combo.Focus();
        combo.IsDropDownOpen = true;
        for (var i = 0; i <= index; i++)
            HeadlessWindowExtensions.KeyPress(window, Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, string.Empty);
        HeadlessWindowExtensions.KeyPress(window, Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
    }

    internal void Save()
    {
        Click(SaveStore);
    }

    internal void SaveBlueprintChanges() =>
        Click(SaveBlueprint);

    internal void TypeBlueprintName(string name)
    {
        var textBox = BlueprintName;
        textBox.Focus();
        textBox.SelectAll();
        HeadlessWindowExtensions.KeyTextInput(window, name);
    }

    internal T Find<T>(string automationId) where T : Control
    {
        var match = window.GetVisualDescendants()
            .OfType<T>()
            .SingleOrDefault(control => AutomationProperties.GetAutomationId(control) == automationId);
        return match ?? throw new InvalidOperationException($"Could not locate Store Editor control '{automationId}'.");
    }

    private void Click(Control control)
    {
        control.Focus();
        HeadlessWindowExtensions.KeyPress(window, Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
    }
}
