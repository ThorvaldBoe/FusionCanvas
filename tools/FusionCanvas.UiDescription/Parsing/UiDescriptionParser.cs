using System.Globalization;
using System.Text.RegularExpressions;
using FusionCanvas.UiDescription.Diagnostics;
using FusionCanvas.UiDescription.Model;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace FusionCanvas.UiDescription.Parsing;

public sealed partial class UiDescriptionParser
{
    private static readonly HashSet<string> DocumentKeys = ["schemaVersion", "tokenProfile", "screen", "states"];
    private static readonly HashSet<string> ScreenKeys = ["id", "title", "viewport", "root"];
    private static readonly HashSet<string> ViewportKeys = ["width", "height"];
    private static readonly HashSet<string> ComponentKeys =
    [
        "id", "kind", "variant", "text", "width", "height", "minWidth", "minHeight",
        "gap", "padding", "axis", "align", "columns", "rowTracks", "column", "row",
        "columnSpan", "rowSpan", "children", "items", "tableColumns", "tableRows", "visible", "enabled"
    ];
    private static readonly HashSet<string> TableColumnKeys = ["header", "width"];
    private static readonly HashSet<string> StateKeys = ["overrides"];
    private static readonly HashSet<string> OverrideKeys = ["target", "visible", "enabled", "text", "items", "tableRows"];

    public UiParseResult ParseFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            var source = File.ReadAllText(path);
            return Parse(source, Path.GetFullPath(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var diagnostic = new UiDiagnostic(
                "UIDL001",
                UiDiagnosticSeverity.Error,
                UiSourceLocation.Unknown(path),
                exception.Message);
            return new UiParseResult(null, [diagnostic]);
        }
    }

    public UiParseResult Parse(string source, string sourcePath = "<memory>")
    {
        ArgumentNullException.ThrowIfNull(source);
        var diagnostics = new List<UiDiagnostic>();

        var unsupportedMatch = UnsupportedYamlFeatureRegex().Match(source);
        if (unsupportedMatch.Success)
        {
            var line = source.AsSpan(0, unsupportedMatch.Index).Count('\n') + 1;
            diagnostics.Add(Error("UIDL003", sourcePath, line, 1, "YAML aliases, anchors, and custom tags are not supported."));
            return new UiParseResult(null, diagnostics);
        }

        var yaml = new YamlStream();
        try
        {
            yaml.Load(new StringReader(source));
        }
        catch (YamlException exception)
        {
            diagnostics.Add(Error(
                "UIDL002",
                sourcePath,
                checked((int)exception.Start.Line + 1),
                checked((int)exception.Start.Column + 1),
                exception.Message));
            return new UiParseResult(null, diagnostics);
        }
        catch (ArgumentException exception)
        {
            diagnostics.Add(Error("UIDL004", sourcePath, 1, 1, $"Duplicate YAML mapping key: {exception.Message}"));
            return new UiParseResult(null, diagnostics);
        }

        if (yaml.Documents.Count != 1)
        {
            diagnostics.Add(Error("UIDL005", sourcePath, 1, 1, "Exactly one YAML document is required."));
            return new UiParseResult(null, diagnostics);
        }

        if (yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            diagnostics.Add(At("UIDL006", sourcePath, yaml.Documents[0].RootNode, "The document root must be a mapping."));
            return new UiParseResult(null, diagnostics);
        }

        var rootValues = ReadKnownMapping(root, DocumentKeys, sourcePath, diagnostics);
        var schemaVersion = ReadRequiredInt(rootValues, "schemaVersion", sourcePath, diagnostics);
        var tokenProfile = ReadRequiredString(rootValues, "tokenProfile", sourcePath, diagnostics);
        var screen = rootValues.TryGetValue("screen", out var screenNode)
            ? ParseScreen(screenNode, sourcePath, diagnostics)
            : MissingScreen(sourcePath, root, diagnostics);
        var states = rootValues.TryGetValue("states", out var statesNode)
            ? ParseStates(statesNode, sourcePath, diagnostics)
            : new Dictionary<string, UiState>(StringComparer.Ordinal);

        if (diagnostics.Count > 0 || screen is null)
        {
            diagnostics.Sort();
            return new UiParseResult(null, diagnostics);
        }

        return new UiParseResult(
            new UiDescriptionDocument(
                schemaVersion,
                tokenProfile,
                screen,
                states,
                Location(sourcePath, root)),
            []);
    }

    private static UiScreen? ParseScreen(YamlNode node, string path, List<UiDiagnostic> diagnostics)
    {
        if (node is not YamlMappingNode mapping)
        {
            diagnostics.Add(At("UIDL006", path, node, "'screen' must be a mapping."));
            return null;
        }

        var values = ReadKnownMapping(mapping, ScreenKeys, path, diagnostics);
        var id = ReadRequiredString(values, "id", path, diagnostics);
        var title = ReadRequiredString(values, "title", path, diagnostics);
        decimal width = 0;
        decimal height = 0;
        if (values.TryGetValue("viewport", out var viewportNode) && viewportNode is YamlMappingNode viewport)
        {
            var viewportValues = ReadKnownMapping(viewport, ViewportKeys, path, diagnostics);
            width = ReadRequiredDecimal(viewportValues, "width", path, diagnostics);
            height = ReadRequiredDecimal(viewportValues, "height", path, diagnostics);
        }
        else
        {
            diagnostics.Add(At("UIDL007", path, viewportNode ?? mapping, "A viewport mapping is required."));
        }

        UiComponent? root = null;
        if (values.TryGetValue("root", out var rootNode))
        {
            root = ParseComponent(rootNode, path, diagnostics);
        }
        else
        {
            diagnostics.Add(At("UIDL007", path, mapping, "A root component is required."));
        }

        return root is null ? null : new UiScreen(id, title, width, height, root, Location(path, mapping));
    }

    private static UiComponent? ParseComponent(YamlNode node, string path, List<UiDiagnostic> diagnostics)
    {
        if (node is not YamlMappingNode mapping)
        {
            diagnostics.Add(At("UIDL006", path, node, "A component must be a mapping."));
            return null;
        }

        var values = ReadKnownMapping(mapping, ComponentKeys, path, diagnostics);
        var id = ReadRequiredString(values, "id", path, diagnostics);
        var kind = ReadRequiredString(values, "kind", path, diagnostics);

        return new UiComponent
        {
            Id = id,
            Kind = kind,
            Variant = ReadOptionalString(values, "variant", path, diagnostics),
            Text = ReadOptionalString(values, "text", path, diagnostics),
            Width = ReadOptionalLength(values, "width", path, diagnostics),
            Height = ReadOptionalLength(values, "height", path, diagnostics),
            MinWidth = ReadOptionalDecimal(values, "minWidth", path, diagnostics),
            MinHeight = ReadOptionalDecimal(values, "minHeight", path, diagnostics),
            Gap = ReadOptionalString(values, "gap", path, diagnostics),
            Padding = ReadOptionalString(values, "padding", path, diagnostics),
            Axis = ReadOptionalString(values, "axis", path, diagnostics),
            Align = ReadOptionalString(values, "align", path, diagnostics),
            Columns = ReadLengths(values, "columns", path, diagnostics),
            RowTracks = ReadLengths(values, "rowTracks", path, diagnostics),
            Column = ReadOptionalInt(values, "column", path, diagnostics),
            Row = ReadOptionalInt(values, "row", path, diagnostics),
            ColumnSpan = ReadOptionalInt(values, "columnSpan", path, diagnostics) ?? 1,
            RowSpan = ReadOptionalInt(values, "rowSpan", path, diagnostics) ?? 1,
            Children = ReadComponents(values, "children", path, diagnostics),
            Items = ReadStrings(values, "items", path, diagnostics),
            TableColumns = ReadTableColumns(values, "tableColumns", path, diagnostics),
            TableRows = ReadStringRows(values, "tableRows", path, diagnostics),
            Visible = ReadOptionalBool(values, "visible", path, diagnostics) ?? true,
            Enabled = ReadOptionalBool(values, "enabled", path, diagnostics) ?? true,
            Source = Location(path, mapping)
        };
    }

    private static IReadOnlyDictionary<string, UiState> ParseStates(YamlNode node, string path, List<UiDiagnostic> diagnostics)
    {
        var states = new Dictionary<string, UiState>(StringComparer.Ordinal);
        if (node is not YamlMappingNode mapping)
        {
            diagnostics.Add(At("UIDL006", path, node, "'states' must be a mapping."));
            return states;
        }

        foreach (var pair in mapping.Children)
        {
            if (pair.Key is not YamlScalarNode nameNode || string.IsNullOrWhiteSpace(nameNode.Value))
            {
                diagnostics.Add(At("UIDL008", path, pair.Key, "State names must be non-empty strings."));
                continue;
            }

            if (pair.Value is not YamlMappingNode stateMapping)
            {
                diagnostics.Add(At("UIDL006", path, pair.Value, $"State '{nameNode.Value}' must be a mapping."));
                continue;
            }

            var values = ReadKnownMapping(stateMapping, StateKeys, path, diagnostics);
            var overrides = values.TryGetValue("overrides", out var overridesNode)
                ? ParseOverrides(overridesNode, path, diagnostics)
                : [];
            states[nameNode.Value] = new UiState(nameNode.Value, overrides, Location(path, nameNode));
        }

        return states;
    }

    private static IReadOnlyList<UiStateOverride> ParseOverrides(YamlNode node, string path, List<UiDiagnostic> diagnostics)
    {
        if (node is not YamlSequenceNode sequence)
        {
            diagnostics.Add(At("UIDL006", path, node, "State overrides must be a sequence."));
            return [];
        }

        var overrides = new List<UiStateOverride>();
        foreach (var item in sequence.Children)
        {
            if (item is not YamlMappingNode mapping)
            {
                diagnostics.Add(At("UIDL006", path, item, "A state override must be a mapping."));
                continue;
            }

            var values = ReadKnownMapping(mapping, OverrideKeys, path, diagnostics);
            overrides.Add(new UiStateOverride(
                ReadRequiredString(values, "target", path, diagnostics),
                ReadOptionalBool(values, "visible", path, diagnostics),
                ReadOptionalBool(values, "enabled", path, diagnostics),
                ReadOptionalString(values, "text", path, diagnostics),
                values.ContainsKey("items") ? ReadStrings(values, "items", path, diagnostics) : null,
                values.ContainsKey("tableRows") ? ReadStringRows(values, "tableRows", path, diagnostics) : null,
                Location(path, mapping)));
        }

        return overrides;
    }

    private static IReadOnlyList<UiComponent> ReadComponents(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return [];
        }

        if (node is not YamlSequenceNode sequence)
        {
            diagnostics.Add(At("UIDL006", path, node, $"'{key}' must be a sequence."));
            return [];
        }

        return sequence.Children
            .Select(child => ParseComponent(child, path, diagnostics))
            .Where(component => component is not null)
            .Cast<UiComponent>()
            .ToArray();
    }

    private static IReadOnlyList<UiTableColumn> ReadTableColumns(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return [];
        }

        if (node is not YamlSequenceNode sequence)
        {
            diagnostics.Add(At("UIDL006", path, node, $"'{key}' must be a sequence."));
            return [];
        }

        var columns = new List<UiTableColumn>();
        foreach (var child in sequence.Children)
        {
            if (child is not YamlMappingNode mapping)
            {
                diagnostics.Add(At("UIDL006", path, child, "A table column must be a mapping."));
                continue;
            }

            var columnValues = ReadKnownMapping(mapping, TableColumnKeys, path, diagnostics);
            columns.Add(new UiTableColumn(
                ReadRequiredString(columnValues, "header", path, diagnostics),
                ReadOptionalLength(columnValues, "width", path, diagnostics) ?? UiLength.Fill,
                Location(path, mapping)));
        }

        return columns;
    }

    private static IReadOnlyList<UiLength> ReadLengths(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return [];
        }

        if (node is not YamlSequenceNode sequence)
        {
            diagnostics.Add(At("UIDL006", path, node, $"'{key}' must be a sequence."));
            return [];
        }

        var lengths = new List<UiLength>();
        foreach (var child in sequence.Children)
        {
            if (!TryScalar(child, out var scalar) || !UiLength.TryParse(scalar, out var length))
            {
                diagnostics.Add(At("UIDL009", path, child, $"'{key}' contains an invalid length."));
                continue;
            }

            lengths.Add(length);
        }

        return lengths;
    }

    private static IReadOnlyList<string> ReadStrings(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return [];
        }

        if (node is not YamlSequenceNode sequence)
        {
            diagnostics.Add(At("UIDL006", path, node, $"'{key}' must be a sequence."));
            return [];
        }

        var result = new List<string>();
        foreach (var child in sequence.Children)
        {
            if (!TryScalar(child, out var scalar))
            {
                diagnostics.Add(At("UIDL010", path, child, $"'{key}' entries must be scalar strings."));
                continue;
            }

            result.Add(scalar!);
        }

        return result;
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadStringRows(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return [];
        }

        if (node is not YamlSequenceNode rows)
        {
            diagnostics.Add(At("UIDL006", path, node, $"'{key}' must be a sequence of rows."));
            return [];
        }

        var result = new List<IReadOnlyList<string>>();
        foreach (var rowNode in rows.Children)
        {
            if (rowNode is not YamlSequenceNode row)
            {
                diagnostics.Add(At("UIDL006", path, rowNode, $"'{key}' rows must be sequences."));
                continue;
            }

            var cells = new List<string>();
            foreach (var cell in row.Children)
            {
                if (!TryScalar(cell, out var scalar))
                {
                    diagnostics.Add(At("UIDL010", path, cell, "Table cells must be scalar strings."));
                    continue;
                }

                cells.Add(scalar!);
            }

            result.Add(cells);
        }

        return result;
    }

    private static Dictionary<string, YamlNode> ReadKnownMapping(
        YamlMappingNode mapping,
        IReadOnlySet<string> allowed,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        var values = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
        foreach (var pair in mapping.Children)
        {
            if (pair.Key is not YamlScalarNode keyNode || string.IsNullOrWhiteSpace(keyNode.Value))
            {
                diagnostics.Add(At("UIDL008", path, pair.Key, "Mapping keys must be non-empty strings."));
                continue;
            }

            if (!allowed.Contains(keyNode.Value))
            {
                diagnostics.Add(At("UIDL011", path, keyNode, $"Unknown property '{keyNode.Value}'.", keyNode.Value));
                continue;
            }

            values[keyNode.Value] = pair.Value;
        }

        return values;
    }

    private static string ReadRequiredString(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            diagnostics.Add(Error("UIDL007", path, 1, 1, $"Required property '{key}' is missing.", key));
            return string.Empty;
        }

        if (!TryScalar(node, out var scalar) || string.IsNullOrWhiteSpace(scalar))
        {
            diagnostics.Add(At("UIDL010", path, node, $"'{key}' must be a non-empty scalar string.", key));
            return string.Empty;
        }

        return scalar;
    }

    private static string? ReadOptionalString(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return null;
        }

        if (!TryScalar(node, out var scalar))
        {
            diagnostics.Add(At("UIDL010", path, node, $"'{key}' must be a scalar string.", key));
            return null;
        }

        return scalar;
    }

    private static int ReadRequiredInt(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.ContainsKey(key))
        {
            return MissingInt(key, path, diagnostics);
        }

        return ReadOptionalInt(values, key, path, diagnostics) ?? 0;
    }

    private static int MissingInt(string key, string path, List<UiDiagnostic> diagnostics)
    {
        diagnostics.Add(Error("UIDL007", path, 1, 1, $"Required integer property '{key}' is missing.", key));
        return 0;
    }

    private static int? ReadOptionalInt(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return null;
        }

        if (!TryScalar(node, out var scalar) || !int.TryParse(scalar, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            diagnostics.Add(At("UIDL012", path, node, $"'{key}' must be an integer.", key));
            return null;
        }

        return result;
    }

    private static decimal ReadRequiredDecimal(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.ContainsKey(key))
        {
            return MissingDecimal(key, path, diagnostics);
        }

        return ReadOptionalDecimal(values, key, path, diagnostics) ?? 0;
    }

    private static decimal MissingDecimal(string key, string path, List<UiDiagnostic> diagnostics)
    {
        diagnostics.Add(Error("UIDL007", path, 1, 1, $"Required numeric property '{key}' is missing.", key));
        return 0;
    }

    private static decimal? ReadOptionalDecimal(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return null;
        }

        if (!TryScalar(node, out var scalar) || !decimal.TryParse(scalar, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            diagnostics.Add(At("UIDL013", path, node, $"'{key}' must be a finite invariant number.", key));
            return null;
        }

        return result;
    }

    private static bool? ReadOptionalBool(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return null;
        }

        if (!TryScalar(node, out var scalar) || !bool.TryParse(scalar, out var result))
        {
            diagnostics.Add(At("UIDL014", path, node, $"'{key}' must be true or false.", key));
            return null;
        }

        return result;
    }

    private static UiLength? ReadOptionalLength(
        IReadOnlyDictionary<string, YamlNode> values,
        string key,
        string path,
        List<UiDiagnostic> diagnostics)
    {
        if (!values.TryGetValue(key, out var node))
        {
            return null;
        }

        if (!TryScalar(node, out var scalar) || !UiLength.TryParse(scalar, out var length))
        {
            diagnostics.Add(At("UIDL009", path, node, $"'{key}' must be 'content', 'fill', or an invariant number.", key));
            return null;
        }

        return length;
    }

    private static UiScreen? MissingScreen(string path, YamlNode node, List<UiDiagnostic> diagnostics)
    {
        diagnostics.Add(At("UIDL007", path, node, "A screen definition is required."));
        return null;
    }

    private static bool TryScalar(YamlNode node, out string? value)
    {
        value = (node as YamlScalarNode)?.Value;
        return node is YamlScalarNode;
    }

    private static UiSourceLocation Location(string path, YamlNode node) =>
        new(path, checked((int)node.Start.Line + 1), checked((int)node.Start.Column + 1));

    private static UiDiagnostic At(string code, string path, YamlNode node, string message, string? subject = null) =>
        new(code, UiDiagnosticSeverity.Error, Location(path, node), message, subject);

    private static UiDiagnostic Error(string code, string path, int line, int column, string message, string? subject = null) =>
        new(code, UiDiagnosticSeverity.Error, new UiSourceLocation(path, line, column), message, subject);

    [GeneratedRegex(@"(?m)(^|\s)(?:[&*][A-Za-z_][\w-]*|![A-Za-z_][\w!-]*)(?=$|\s)", RegexOptions.CultureInvariant)]
    private static partial Regex UnsupportedYamlFeatureRegex();
}
