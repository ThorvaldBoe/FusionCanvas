using System.Xml.Linq;
using FusionCanvas.UiDescription.Layout;
using FusionCanvas.UiDescription.Parsing;
using FusionCanvas.UiDescription.Rendering;
using FusionCanvas.UiDescription.Validation;

namespace FusionCanvas.UiDescription.Tests;

public sealed class StateLayoutAndRendererTests
{
    [Fact]
    public void State_projection_changes_only_declared_values()
    {
        var document = TestSupport.ParseValidated();

        var result = new UiStateProjector().Project(document, "disabled");

        Assert.True(result.IsValid);
        var before = TestSupport.Flatten(document.Screen.Root).ToDictionary(item => item.Id);
        var after = TestSupport.Flatten(result.Document!.Screen.Root).ToDictionary(item => item.Id);
        Assert.Equal(before.Keys, after.Keys);
        Assert.Equal("button", after["action"].Kind);
        Assert.Equal("primary", after["action"].Variant);
        Assert.False(after["action"].Enabled);
        Assert.Equal("Unavailable", after["action"].Text);
        Assert.Equal(before["heading"], after["heading"]);
    }

    [Fact]
    public void Unknown_state_fails_before_layout()
    {
        var result = new UiStateProjector().Project(TestSupport.ParseValidated(), "missing");

        Assert.False(result.IsValid);
        Assert.Equal("UIDL130", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void Text_measurement_counts_unicode_scalars_deterministically()
    {
        var tokens = new WireframeTokenProfile();

        var emoji = tokens.MeasureText("😀", "body");
        var letter = tokens.MeasureText("A", "body");

        Assert.Equal(letter.Width, emoji.Width);
        Assert.Equal(letter.Height, emoji.Height);
    }

    [Fact]
    public void Stack_layout_is_repeatable_and_contained_by_the_viewport()
    {
        var document = TestSupport.ParseValidated();
        var engine = new UiLayoutEngine();

        var first = engine.Layout(document);
        var second = engine.Layout(document);

        Assert.True(first.IsValid);
        var renderer = new SvgWireframeRenderer();
        Assert.Equal(
            renderer.Render(first.Root!, 640, 480, document.Screen.Title),
            renderer.Render(second.Root!, 640, 480, document.Screen.Title));
        Assert.All(Flatten(first.Root!), node =>
        {
            Assert.True(node.Bounds.X >= 0);
            Assert.True(node.Bounds.Y >= 0);
            Assert.True(node.Bounds.Right <= document.Screen.ViewportWidth);
            Assert.True(node.Bounds.Bottom <= document.Screen.ViewportHeight);
        });
    }

    [Fact]
    public void Renderer_emits_canonical_self_contained_svg()
    {
        var source = TestSupport.MinimalYaml.Replace("Sample heading", "A & <B>", StringComparison.Ordinal);
        var document = TestSupport.ParseValidated(source);
        var layout = new UiLayoutEngine().Layout(document);
        var renderer = new SvgWireframeRenderer();

        var first = renderer.Render(layout.Root!, 640, 480, document.Screen.Title);
        var second = renderer.Render(layout.Root!, 640, 480, document.Screen.Title);

        Assert.Equal(first, second);
        Assert.DoesNotContain('\r', first);
        Assert.DoesNotContain("<script", first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", first.Replace("http://www.w3.org/2000/svg", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-ui-id=\"heading\"", first, StringComparison.Ordinal);
        Assert.Contains("A &amp; &lt;B&gt;", first, StringComparison.Ordinal);
        _ = XDocument.Parse(first);
    }

    [Fact]
    public void Design_areas_collection_uses_the_full_management_surface()
    {
        var source = Path.Combine(TestSupport.RepositoryRoot, "docs", "Visuals", "ui-descriptions", "manage-design-areas.ui.yaml");
        var parsed = new UiDescriptionParser().ParseFile(source);
        var validated = new UiDescriptionValidator().Validate(parsed.Document!);
        var layout = new UiLayoutEngine().Layout(validated.Document!);
        var nodes = Flatten(layout.Root!).ToDictionary(node => node.Component.Id);

        Assert.Equal(1104, nodes["area-collection"].Bounds.Width);
        Assert.DoesNotContain("area-editor-dialog", nodes.Keys);
        Assert.All(nodes.Values, node => Assert.True(node.Bounds.Right <= validated.Document!.Screen.ViewportWidth));
    }

    private static IEnumerable<UiLayoutNode> Flatten(UiLayoutNode root)
    {
        yield return root;
        foreach (var child in root.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }
}
