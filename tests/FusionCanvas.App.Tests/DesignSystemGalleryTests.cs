using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FusionCanvas.App.DesignSystem;

namespace FusionCanvas.App.Tests;

public sealed class DesignSystemGalleryTests
{
    [AvaloniaFact]
    public void Gallery_ExposesTokenFamiliesControlsStatesAndOverviewComposition()
    {
        var window = new DesignSystemGalleryWindow();

        try
        {
            window.Show();
            PumpLayout(window);

            Assert.NotNull(FindByAutomationId(window, "DesignSystemGallery.ColorTokens"));
            Assert.NotNull(FindByAutomationId(window, "DesignSystemGallery.TypographyTokens"));
            Assert.NotNull(FindByAutomationId(window, "DesignSystemGallery.SpacingTokens"));
            Assert.NotNull(FindByAutomationId(window, "DesignSystemGallery.Controls"));
            Assert.NotNull(FindByAutomationId(window, "DesignSystemGallery.OverviewSample"));
            Assert.NotNull(FindText(window, "Title — Concept Overview"));
            Assert.NotNull(FindText(window, "Destructive"));
            Assert.NotNull(FindText(window, "Selected state"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TokenRegistry_ProvidesLightAndDarkSemanticAccentTokens()
    {
        var resources = global::Avalonia.Application.Current!.Resources;

        Assert.True(resources.TryGetResource("Token.Color.Accent", ThemeVariant.Light, out var lightAccent));
        Assert.True(resources.TryGetResource("Token.Color.Accent", ThemeVariant.Dark, out var darkAccent));
        Assert.NotNull(lightAccent);
        Assert.NotNull(darkAccent);
        Assert.NotSame(lightAccent, darkAccent);
        Assert.True(resources.TryGetResource("Token.Typography.SectionHeading", ThemeVariant.Light, out _));
        Assert.True(resources.TryGetResource("Token.Spacing.SectionGap", ThemeVariant.Light, out _));
        Assert.True(resources.TryGetResource("Token.Radius.Card", ThemeVariant.Light, out _));
        Assert.True(resources.TryGetResource("Token.Control.MinHeight", ThemeVariant.Light, out _));
        Assert.True(resources.TryGetResource("Token.Border.Default", ThemeVariant.Light, out _));
    }

    [AvaloniaFact]
    public void ConceptOverview_UsesSharedSurfaceTypographySpacingAndControlTokens()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();

        var overview = FindByAutomationId(fixture.Window, "ConceptOverview.Surface") as Border;
        Assert.NotNull(overview);
        Assert.Equal(14, overview!.Padding.Left);
        Assert.Equal(6, overview.CornerRadius.TopLeft);
        Assert.Equal(1, overview.BorderThickness.Left);

        var heading = overview.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(text => text.Text == "Overview");
        Assert.NotNull(heading);
        Assert.Equal(14, heading!.FontSize);

        var titleBox = overview.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(text => AutomationProperties.GetName(text) == "Item working title");
        Assert.NotNull(titleBox);
        Assert.Equal(32, titleBox!.MinHeight);
    }

    private static Control? FindByAutomationId(Window window, string automationId) =>
        window.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control => AutomationProperties.GetAutomationId(control) == automationId);

    private static TextBlock? FindText(Window window, string text) =>
        window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(block => block.Text == text);

    private static void PumpLayout(Window window)
    {
        window.UpdateLayout();
        window.UpdateLayout();
    }
}
