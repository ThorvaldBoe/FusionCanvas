using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
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

    [AvaloniaFact]
    public void WithoutImage_PointerDoesNotMoveOrResizePlacement()
    {
        var editor = new MockupPlacementEditor
        {
            Width = 400,
            Height = 400,
            ImageWidth = 0,
            ImageHeight = 0,
            PlacementX = 0,
            PlacementY = 0,
            PlacementWidth = 100,
            PlacementHeight = 100
        };
        var window = Show(editor);

        HeadlessWindowExtensions.MouseDown(window, new Point(50, 50), MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseMove(window, new Point(250, 250), RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(window, new Point(250, 250), MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(0, editor.PlacementX);
        Assert.Equal(0, editor.PlacementY);
        Assert.Equal(100, editor.PlacementWidth);
        Assert.Equal(100, editor.PlacementHeight);
        window.Close();
    }

    [AvaloniaFact]
    public void ShrinkingImageBoundsClampsPlacementInsideImage()
    {
        var editor = NewEditor();
        var window = Show(editor);

        editor.ImageWidth = 400;

        Assert.Equal(0, editor.PlacementX);
        Assert.Equal(400, editor.PlacementWidth);
        Assert.True(editor.PlacementX + editor.PlacementWidth <= 400);
        Assert.True(editor.PlacementY + editor.PlacementHeight <= 1000);
        window.Close();
    }

    [AvaloniaFact]
    public void LetterboxedPreview_MapsPointerDragWithUniformScale()
    {
        var editor = new MockupPlacementEditor
        {
            Width = 400,
            Height = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            ImageWidth = 1000,
            ImageHeight = 1000,
            PlacementX = 250,
            PlacementY = 250,
            PlacementWidth = 500,
            PlacementHeight = 500
        };
        var window = Show(editor);

        var start = editor.TranslatePoint(new Point(160, 60), window) ?? new Point(160, 60);
        var end = editor.TranslatePoint(new Point(200, 80), window) ?? new Point(200, 80);
        HeadlessWindowExtensions.MouseDown(window, start, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseMove(window, end, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(window, end, MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(450, editor.PlacementX, 1);
        Assert.Equal(350, editor.PlacementY, 1);
        Assert.Equal(500, editor.PlacementWidth, 1);
        Assert.Equal(500, editor.PlacementHeight, 1);
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
