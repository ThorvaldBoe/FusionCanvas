using FusionCanvas.UiDescription.Parsing;
using FusionCanvas.UiDescription.Validation;

namespace FusionCanvas.UiDescription.Tests;

public sealed class ParsingAndValidationTests
{
    [Fact]
    public void Parser_accepts_supported_document_and_captures_locations()
    {
        var result = new UiDescriptionParser().Parse(TestSupport.MinimalYaml, "sample.ui.yaml");

        Assert.True(result.IsValid);
        Assert.Equal("sample", result.Document!.Screen.Id);
        Assert.Equal("sample.ui.yaml", result.Document.Screen.Root.Source.Path);
        Assert.True(result.Document.Screen.Root.Source.Line > 0);
    }

    [Theory]
    [InlineData("unexpected: true", "UIDL011")]
    [InlineData("schemaVersion: 2", "UIDL100")]
    [InlineData("schemaVersion: nope", "UIDL012")]
    public void Invalid_documents_report_stable_diagnostics(string replacement, string expectedCode)
    {
        var source = replacement.StartsWith("schemaVersion", StringComparison.Ordinal)
            ? TestSupport.MinimalYaml.Replace("schemaVersion: 1", replacement, StringComparison.Ordinal)
            : TestSupport.MinimalYaml + Environment.NewLine + replacement;

        var parsed = new UiDescriptionParser().Parse(source, "invalid.ui.yaml");
        var diagnostics = parsed.IsValid
            ? new UiDescriptionValidator().Validate(parsed.Document!).Diagnostics
            : parsed.Diagnostics;

        Assert.Contains(diagnostics, item => item.Code == expectedCode);
        Assert.All(diagnostics, item => Assert.Equal("invalid.ui.yaml", item.Location.Path));
    }

    [Theory]
    [InlineData("schemaVersion: &version 1\ncopy: *version", "UIDL003")]
    [InlineData("schemaVersion: !custom 1", "UIDL003")]
    [InlineData("schemaVersion: 1\nschemaVersion: 1", "UIDL002")]
    [InlineData("schemaVersion: 1\n---\nschemaVersion: 1", "UIDL005")]
    public void Unsupported_yaml_constructs_are_rejected(string source, string expectedCode)
    {
        var result = new UiDescriptionParser().Parse(source);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == expectedCode);
    }

    [Fact]
    public void Validator_reports_duplicate_ids_and_invalid_vocabulary_in_source_order()
    {
        var source = TestSupport.MinimalYaml
            .Replace("id: action", "id: heading", StringComparison.Ordinal)
            .Replace("variant: primary", "variant: imaginary", StringComparison.Ordinal)
            .Replace("gap: section", "gap: unknown-space", StringComparison.Ordinal);
        var parsed = new UiDescriptionParser().Parse(source);

        var first = new UiDescriptionValidator().Validate(parsed.Document!).Diagnostics;
        var second = new UiDescriptionValidator().Validate(parsed.Document!).Diagnostics;

        Assert.Equal(first, second);
        Assert.Contains(first, item => item.Code == "UIDL105" && item.Subject == "heading");
        Assert.Contains(first, item => item.Code == "UIDL107");
        Assert.Contains(first, item => item.Code == "UIDL109");
    }

    [Fact]
    public void Validator_rejects_unknown_state_targets_and_incompatible_overrides()
    {
        var source = TestSupport.MinimalYaml
            .Replace("target: action", "target: missing", StringComparison.Ordinal)
            .Replace("text: Unavailable", "text: Unavailable\n      - target: heading\n        enabled: false", StringComparison.Ordinal);
        var parsed = new UiDescriptionParser().Parse(source);

        var result = new UiDescriptionValidator().Validate(parsed.Document!);

        Assert.Contains(result.Diagnostics, item => item.Code == "UIDL122" && item.Subject == "missing");
        Assert.Contains(result.Diagnostics, item => item.Code == "UIDL123" && item.Subject == "heading");
    }

    [Fact]
    public void Validator_rejects_invalid_sizing_and_leaf_composition_before_layout()
    {
        var source = TestSupport.MinimalYaml
            .Replace("text: Continue", "text: Continue\n        height: -1\n        children:\n          - { id: nested, kind: text, text: Invalid }", StringComparison.Ordinal);
        var parsed = new UiDescriptionParser().Parse(source);

        var result = new UiDescriptionValidator().Validate(parsed.Document!);

        Assert.Contains(result.Diagnostics, item => item.Code == "UIDL108" && item.Subject == "action");
        Assert.Contains(result.Diagnostics, item => item.Code == "UIDL110" && item.Subject == "action");
    }
}
