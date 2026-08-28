using System.Security.Cryptography;
using FusionCanvas.UiDescription.Commands;
using FusionCanvas.UiDescription.Layout;
using FusionCanvas.UiDescription.Parsing;
using FusionCanvas.UiDescription.Rendering;
using FusionCanvas.UiDescription.Validation;

namespace FusionCanvas.UiDescription.Tests;

public sealed class FixtureAndCliTests
{
    public static TheoryData<string, string, string[]> Fixtures => new()
    {
        {
            "manage-variants.ui.yaml",
            "manage-variants.default.svg",
            ["page-header", "available-section", "color-card", "size-card", "variants-table"]
        },
        {
            "manage-design-areas.ui.yaml",
            "manage-design-areas.default.svg",
            ["page-header", "area-collection", "front-area-card", "area-editor-dialog", "save-area"]
        }
    };

    public static TheoryData<string, string, string> StateGoldens => new()
    {
        { "manage-variants.ui.yaml", "default", "manage-variants.default.svg" },
        { "manage-variants.ui.yaml", "provider-unavailable", "manage-variants.provider-unavailable.svg" },
        { "manage-design-areas.ui.yaml", "default", "manage-design-areas.default.svg" },
        { "manage-design-areas.ui.yaml", "empty-collection", "manage-design-areas.empty-collection.svg" }
    };

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Issue_185_fixtures_have_required_semantics_and_match_golden_svg(
        string yamlName,
        string svgName,
        string[] requiredIds)
    {
        var directory = Path.Combine(TestSupport.RepositoryRoot, "docs", "Visuals", "ui-descriptions");
        var parsed = new UiDescriptionParser().ParseFile(Path.Combine(directory, yamlName));
        Assert.True(parsed.IsValid, string.Join(Environment.NewLine, parsed.Diagnostics));
        var validated = new UiDescriptionValidator().Validate(parsed.Document!);
        Assert.True(validated.IsValid, string.Join(Environment.NewLine, validated.Diagnostics));
        Assert.Contains("default", validated.Document!.States.Keys);
        Assert.True(validated.Document.States.Count >= 2);

        var components = TestSupport.Flatten(validated.Document.Screen.Root).ToArray();
        Assert.All(requiredIds, id => Assert.Contains(components, item => item.Id == id));
        var layout = new UiLayoutEngine().Layout(validated.Document);
        Assert.True(layout.IsValid, string.Join(Environment.NewLine, layout.Diagnostics));
        var rendered = new SvgWireframeRenderer().Render(
            layout.Root!,
            validated.Document.Screen.ViewportWidth,
            validated.Document.Screen.ViewportHeight,
            validated.Document.Screen.Title);

        Assert.Equal(File.ReadAllText(Path.Combine(directory, svgName)), rendered);
    }

    [Theory]
    [MemberData(nameof(StateGoldens))]
    public async Task Every_declared_fixture_state_matches_its_golden_svg(string yamlName, string state, string svgName)
    {
        var directory = Path.Combine(TestSupport.RepositoryRoot, "docs", "Visuals", "ui-descriptions");
        var destination = Path.Combine(Path.GetTempPath(), $"ui-description-{Guid.NewGuid():N}.svg");
        try
        {
            var exit = await UiDescriptionCli.RunAsync(
                ["render", Path.Combine(directory, yamlName), "--state", state, "--output", destination],
                new StringWriter(),
                new StringWriter());

            Assert.Equal(UiDescriptionCli.Success, exit);
            Assert.Equal(File.ReadAllBytes(Path.Combine(directory, svgName)), File.ReadAllBytes(destination));
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [Fact]
    public async Task Cli_validates_and_renders_without_mutating_source()
    {
        var source = Path.Combine(TestSupport.RepositoryRoot, "docs", "Visuals", "ui-descriptions", "manage-variants.ui.yaml");
        var originalHash = SHA256.HashData(File.ReadAllBytes(source));
        var destination = Path.Combine(Path.GetTempPath(), $"ui-description-{Guid.NewGuid():N}.svg");
        try
        {
            var standardOutput = new StringWriter();
            var errorOutput = new StringWriter();
            var validateExit = await UiDescriptionCli.RunAsync(["validate", source], standardOutput, errorOutput);
            var renderExit = await UiDescriptionCli.RunAsync(
                ["render", source, "--state", "provider-unavailable", "--output", destination],
                standardOutput,
                errorOutput);

            Assert.Equal(UiDescriptionCli.Success, validateExit);
            Assert.Equal(UiDescriptionCli.Success, renderExit);
            Assert.True(File.Exists(destination));
            Assert.Empty(errorOutput.ToString());
            Assert.Equal(originalHash, SHA256.HashData(File.ReadAllBytes(source)));
            var bytes = File.ReadAllBytes(destination);
            Assert.False(bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [Fact]
    public async Task Cli_failure_preserves_existing_destination()
    {
        var source = Path.Combine(TestSupport.RepositoryRoot, "docs", "Visuals", "ui-descriptions", "manage-variants.ui.yaml");
        var destination = Path.Combine(Path.GetTempPath(), $"ui-description-{Guid.NewGuid():N}.svg");
        File.WriteAllText(destination, "keep me");
        try
        {
            var exit = await UiDescriptionCli.RunAsync(
                ["render", source, "--state", "missing", "--output", destination],
                new StringWriter(),
                new StringWriter());

            Assert.Equal(UiDescriptionCli.ValidationFailure, exit);
            Assert.Equal("keep me", File.ReadAllText(destination));
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [Fact]
    public async Task Cli_rejects_invalid_arguments()
    {
        var error = new StringWriter();

        var exit = await UiDescriptionCli.RunAsync(["render"], new StringWriter(), error);

        Assert.Equal(UiDescriptionCli.ValidationFailure, exit);
        Assert.Contains("Usage:", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cli_reports_output_failure_and_cleans_its_temporary_file()
    {
        var source = Path.Combine(TestSupport.RepositoryRoot, "docs", "Visuals", "ui-descriptions", "manage-variants.ui.yaml");
        var destinationDirectory = Path.Combine(Path.GetTempPath(), $"ui-description-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(destinationDirectory);
        try
        {
            var error = new StringWriter();
            var exit = await UiDescriptionCli.RunAsync(
                ["render", source, "--state", "default", "--output", destinationDirectory],
                new StringWriter(),
                error);

            Assert.Equal(UiDescriptionCli.OperationalFailure, exit);
            Assert.Contains("UIDL300", error.ToString(), StringComparison.Ordinal);
            Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(destinationDirectory)!, $".{Path.GetFileName(destinationDirectory)}.*.tmp"));
        }
        finally
        {
            Directory.Delete(destinationDirectory);
        }
    }
}
