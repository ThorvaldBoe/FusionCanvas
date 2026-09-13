using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using FusionCanvas.App.Assets;
using FusionCanvas.Domain.Assets;

namespace FusionCanvas.App.Tests.TestSupport.Drivers;

/// <summary>Drives the rendered Assets surface using the same actions a creator can perform.</summary>
internal sealed class AssetsWindowDriver(AssetsWindow window)
{
    internal Button Import => Find<Button>("Assets.Import");
    internal Button Close => Find<Button>("Assets.Close");

    internal IReadOnlyList<AssetRowViewModel> Rows => window.GetVisualDescendants()
        .OfType<Control>()
        .Select(control => control.DataContext)
        .OfType<AssetRowViewModel>()
        .DistinctBy(row => row.Id)
        .ToArray();

    internal bool IsImportPending => FindVisible<Control>("Assets.PendingPurpose") is not null;
    internal bool IsRemovalConfirmationVisible => FindVisible<Control>("Assets.CancelRemove") is not null;

    internal void ImportFile() => Click(Import);

    internal void ConfirmImport() => Click(Find<Button>("Assets.ConfirmImport"));

    internal void CancelImport() => Click(Find<Button>("Assets.CancelImport"));

    internal void SelectPendingPurpose(AssetKind kind) =>
        SelectPurpose(Find<ComboBox>("Assets.PendingPurpose"), kind);

    internal void SelectPurpose(AssetRowViewModel row, AssetKind kind)
    {
        var combo = window.GetVisualDescendants()
            .OfType<ComboBox>()
            .First(control => ReferenceEquals(control.DataContext, row)
                && AutomationProperties.GetAutomationId(control) == "Assets.Purpose"
                && IsEffectivelyVisible(control));
        SelectPurpose(combo, kind);
    }

    internal async Task<AssetPreviewWindow> OpenPreviewAsync(AssetRowViewModel row)
    {
        var previewButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => ReferenceEquals(button.DataContext, row)
                && AutomationProperties.GetAutomationId(button) == "Assets.Preview");
        Click(previewButton);
        await HeadlessUiWait.UntilAsync(
            () => window.OwnedWindows.OfType<AssetPreviewWindow>().Any(preview => preview.IsVisible),
            "asset preview opens");
        return window.OwnedWindows.OfType<AssetPreviewWindow>().Single(preview => preview.IsVisible);
    }

    internal void ClosePreview(AssetPreviewWindow preview)
    {
        var close = preview.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => string.Equals(button.Content as string, "Close", StringComparison.Ordinal));
        close.Focus();
        HeadlessWindowExtensions.KeyPress(preview, Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
    }

    internal void RequestRemoval(AssetRowViewModel row)
    {
        var removeButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => ReferenceEquals(button.DataContext, row)
                && AutomationProperties.GetAutomationId(button) == "Assets.Remove");
        Click(removeButton);
    }

    internal void CancelRemoval() => Click(Find<Button>("Assets.CancelRemove"));

    internal void CloseSurface() => Click(Close);

    internal T Find<T>(string automationId) where T : Control =>
        FindVisible<T>(automationId)
        ?? throw new InvalidOperationException($"Could not locate visible Assets control '{automationId}'.");

    private T? FindVisible<T>(string automationId) where T : Control => window.GetVisualDescendants()
        .OfType<T>()
        .SingleOrDefault(control => AutomationProperties.GetAutomationId(control) == automationId && IsEffectivelyVisible(control));

    private void SelectPurpose(ComboBox combo, AssetKind kind)
    {
        var targetIndex = combo.Items
            .OfType<AssetPurposeOption>()
            .Select((option, index) => (option, index))
            .Single(value => value.option.Kind == kind)
            .index;
        var currentIndex = combo.SelectedIndex;
        combo.Focus();
        combo.IsDropDownOpen = true;
        var key = targetIndex >= currentIndex ? Key.Down : Key.Up;
        var physical = key == Key.Down ? PhysicalKey.ArrowDown : PhysicalKey.ArrowUp;
        for (var i = 0; i < Math.Abs(targetIndex - currentIndex); i++)
            HeadlessWindowExtensions.KeyPress(window, key, RawInputModifiers.None, physical, string.Empty);
        HeadlessWindowExtensions.KeyPress(window, Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
    }

    private void Click(Control control)
    {
        control.Focus();
        HeadlessWindowExtensions.KeyPress(window, Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
    }

    private static bool IsEffectivelyVisible(Control control)
    {
        for (Control? current = control; current is not null; current = current.Parent as Control)
        {
            if (!current.IsVisible)
                return false;
        }

        return true;
    }
}
