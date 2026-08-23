using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using FusionCanvas.App.Stores;

namespace FusionCanvas.App.Tests;

public sealed class MockupPlacementEditorTests
{
    [AvaloniaFact]
    public void DraggingPlacementRectangleUpdatesImageSpaceCoordinates()
    {
        var editor = NewEditor();
        var window = Show(editor);

        HeadlessWindowExtensions.MouseDown(window, new Point(160, 160), MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseMove(window, new Point(200, 180), RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(window, new Point(200, 180), MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(350, editor.PlacementX, 1);
        Assert.Equal(300, editor.PlacementY, 1);
        Assert.Equal(500, editor.PlacementWidth, 1);
        Assert.Equal(500, editor.PlacementHeight, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingResizeHandleUpdatesDimensionsAndStaysInsideImage()
    {
        var editor = NewEditor();
        var window = Show(editor);

        HeadlessWindowExtensions.MouseDown(window, new Point(300, 300), MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseMove(window, new Point(390, 390), RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(window, new Point(390, 390), MouseButton.Left, RawInputModifiers.None);

        Assert.True(editor.PlacementWidth > 500);
        Assert.True(editor.PlacementHeight > 500);
        Assert.True(editor.PlacementX + editor.PlacementWidth <= 1000);
        Assert.True(editor.PlacementY + editor.PlacementHeight <= 1000);
        window.Close();
    }

    [AvaloniaFact]
    public void ArrowKeysMoveAndShiftArrowResizesInImagePixels()
    {
        var editor = NewEditor();
        var window = Show(editor);
        editor.Focus();

        HeadlessWindowExtensions.KeyPress(window, Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, string.Empty);
        HeadlessWindowExtensions.KeyPress(window, Key.Down, RawInputModifiers.Shift, PhysicalKey.ArrowDown, string.Empty);

        Assert.Equal(251, editor.PlacementX);
        Assert.Equal(501, editor.PlacementHeight);
        window.Close();
    }

    private static MockupPlacementEditor NewEditor() => new()
    {
        Width = 400,
        Height = 400,
        ImageWidth = 1000,
        ImageHeight = 1000,
        PlacementX = 250,
        PlacementY = 250,
        PlacementWidth = 500,
        PlacementHeight = 500
    };

    private static Window Show(Control content)
    {
        var window = new Window { Width = 400, Height = 400, Content = content };
        window.Show();
        window.UpdateLayout();
        return window;
    }
}
