using System.Globalization;
using System.Security;
using System.Text;
using FusionCanvas.UiDescription.Layout;
using FusionCanvas.UiDescription.Model;

namespace FusionCanvas.UiDescription.Rendering;

public sealed class SvgWireframeRenderer(WireframeTokenProfile? tokens = null)
{
    private readonly WireframeTokenProfile tokens = tokens ?? new WireframeTokenProfile();

    public string Render(UiLayoutNode root, decimal viewportWidth, decimal viewportHeight, string title)
    {
        ArgumentNullException.ThrowIfNull(root);
        var output = new StringBuilder();
        AppendLine(output, "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        AppendLine(
            output,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Number(viewportWidth)}\" height=\"{Number(viewportHeight)}\" viewBox=\"0 0 {Number(viewportWidth)} {Number(viewportHeight)}\" role=\"img\" aria-label=\"{Escape(title)}\">");
        AppendLine(output, "  <rect width=\"100%\" height=\"100%\" fill=\"#f4f6f8\" />");
        RenderNode(output, root, 1);
        AppendLine(output, "</svg>");
        return output.ToString();
    }

    private void RenderNode(StringBuilder output, UiLayoutNode node, int depth)
    {
        var indent = new string(' ', depth * 2);
        AppendLine(
            output,
            $"{indent}<g data-ui-id=\"{Escape(node.Component.Id)}\" data-ui-kind=\"{Escape(node.Component.Kind)}\" data-enabled=\"{node.Component.Enabled.ToString().ToLowerInvariant()}\">");

        switch (node.Component.Kind)
        {
            case "panel":
                RenderPanel(output, node, depth + 1);
                break;
            case "text":
                RenderText(output, node, depth + 1);
                break;
            case "field":
            case "select":
                RenderField(output, node, depth + 1);
                break;
            case "button":
                RenderButton(output, node, depth + 1);
                break;
            case "divider":
                RenderDivider(output, node, depth + 1);
                break;
            case "message":
                RenderMessage(output, node, depth + 1);
                break;
            case "list":
                RenderList(output, node, depth + 1);
                break;
            case "table":
                RenderTable(output, node, depth + 1);
                break;
        }

        foreach (var child in node.Children)
        {
            RenderNode(output, child, depth + 1);
        }

        AppendLine(output, $"{indent}</g>");
    }

    private static void RenderPanel(StringBuilder output, UiLayoutNode node, int depth)
    {
        var (fill, stroke, radius) = node.Component.Variant switch
        {
            "choice-card" => ("#ffffff", "#b8c4d2", "9"),
            "summary-card" => ("#ffffff", "#bdc8d5", "9"),
            "canvas" => ("#ffffff", "#b8c4d2", "10"),
            _ => ("#ffffff", "#c3cdd8", "9")
        };
        Rect(output, node.Bounds, fill, stroke, radius, depth);
    }

    private void RenderText(StringBuilder output, UiLayoutNode node, int depth)
    {
        var metric = tokens.ResolveText(node.Component.Variant);
        var fill = node.Component.Variant switch
        {
            "supporting" or "label" => "#617087",
            "link" => "#214f7d",
            "emphasis" => "#153f69",
            _ => "#182536"
        };
        var weight = metric.Bold ? "600" : "400";
        var x = node.Bounds.X;
        var y = node.Bounds.Y + metric.FontSize;
        Text(output, x, y, node.Component.Text ?? string.Empty, metric.FontSize, fill, weight, depth);
    }

    private static void RenderField(StringBuilder output, UiLayoutNode node, int depth)
    {
        Rect(output, node.Bounds, node.Component.Enabled ? "#edf2f7" : "#e4e8ed", "#9fb0c3", "5", depth);
        Text(output, node.Bounds.X + 12, node.Bounds.Y + 24, node.Component.Text ?? string.Empty, 14, node.Component.Enabled ? "#26384c" : "#7c8795", "400", depth);
        if (node.Component.Kind == "select")
        {
            Text(output, node.Bounds.Right - 22, node.Bounds.Y + 24, "▾", 13, "#415870", "600", depth);
        }
    }

    private static void RenderButton(StringBuilder output, UiLayoutNode node, int depth)
    {
        var (fill, stroke, text) = node.Component.Variant switch
        {
            "primary" => ("#214f7d", "#214f7d", "#ffffff"),
            "danger" => ("#a63d35", "#a63d35", "#ffffff"),
            "link" => ("transparent", "transparent", "#214f7d"),
            _ => ("#edf2f7", "#9fb0c3", "#214f7d")
        };
        if (!node.Component.Enabled)
        {
            fill = "#e2e6eb";
            stroke = "#c2c9d1";
            text = "#7f8994";
        }

        Rect(output, node.Bounds, fill, stroke, "7", depth);
        var label = node.Component.Text ?? string.Empty;
        var width = decimal.Ceiling(label.EnumerateRunes().Count() * 14 * 0.56m);
        var x = node.Bounds.X + Math.Max(8, (node.Bounds.Width - width) / 2);
        Text(output, x, node.Bounds.Y + 24, label, 14, text, "600", depth);
    }

    private static void RenderDivider(StringBuilder output, UiLayoutNode node, int depth)
    {
        var indent = new string(' ', depth * 2);
        AppendLine(
            output,
            $"{indent}<line x1=\"{Number(node.Bounds.X)}\" y1=\"{Number(node.Bounds.Y)}\" x2=\"{Number(node.Bounds.Right)}\" y2=\"{Number(node.Bounds.Y)}\" stroke=\"#c5ced8\" stroke-width=\"1\" />");
    }

    private static void RenderMessage(StringBuilder output, UiLayoutNode node, int depth)
    {
        var (fill, stroke, text) = node.Component.Variant switch
        {
            "warning" => ("#fff4dd", "#d4a649", "#765715"),
            "danger" => ("#fdeae8", "#c98078", "#883d35"),
            "empty" => ("#f2f4f7", "#c8d0da", "#617087"),
            _ => ("#eaf2fb", "#a9bfd7", "#315676")
        };
        Rect(output, node.Bounds, fill, stroke, "7", depth);
        Text(output, node.Bounds.X + 12, node.Bounds.Y + 28, node.Component.Text ?? string.Empty, 14, text, "400", depth);
    }

    private static void RenderList(StringBuilder output, UiLayoutNode node, int depth)
    {
        var rowHeight = node.Component.Items.Count == 0 ? node.Bounds.Height : node.Bounds.Height / node.Component.Items.Count;
        for (var index = 0; index < node.Component.Items.Count; index++)
        {
            var row = new UiRect(node.Bounds.X, node.Bounds.Y + (rowHeight * index), node.Bounds.Width, rowHeight);
            Rect(output, row, "#ffffff", "#d0d7e0", "0", depth);
            Text(output, row.X + 12, row.Y + Math.Min(24, row.Height - 8), node.Component.Items[index], 14, "#26384c", "400", depth);
        }
    }

    private static void RenderTable(StringBuilder output, UiLayoutNode node, int depth)
    {
        var columns = ResolveTableColumns(node.Component.TableColumns, node.Bounds.Width);
        var rowHeight = node.Bounds.Height / Math.Max(1, node.Component.TableRows.Count + 1);
        var header = new UiRect(node.Bounds.X, node.Bounds.Y, node.Bounds.Width, rowHeight);
        Rect(output, header, "#e9eef3", "#c2ccd7", "5", depth);
        RenderTableRow(output, header, columns, node.Component.TableColumns.Select(column => column.Header).ToArray(), true, depth + 1);

        for (var rowIndex = 0; rowIndex < node.Component.TableRows.Count; rowIndex++)
        {
            var row = new UiRect(node.Bounds.X, node.Bounds.Y + (rowHeight * (rowIndex + 1)), node.Bounds.Width, rowHeight);
            Rect(output, row, "#ffffff", "#d2d9e1", "0", depth);
            RenderTableRow(output, row, columns, node.Component.TableRows[rowIndex], false, depth + 1);
        }
    }

    private static void RenderTableRow(
        StringBuilder output,
        UiRect row,
        IReadOnlyList<decimal> widths,
        IReadOnlyList<string> cells,
        bool heading,
        int depth)
    {
        var x = row.X;
        for (var index = 0; index < cells.Count && index < widths.Count; index++)
        {
            Text(output, x + 12, row.Y + Math.Min(27, row.Height - 8), cells[index], 13, heading ? "#182536" : "#26384c", heading ? "600" : "400", depth);
            x += widths[index];
        }
    }

    private static decimal[] ResolveTableColumns(IReadOnlyList<UiTableColumn> columns, decimal available)
    {
        var widths = new decimal[columns.Count];
        var fills = new List<int>();
        for (var index = 0; index < columns.Count; index++)
        {
            if (columns[index].Width.Kind == UiLengthKind.Fixed)
            {
                widths[index] = columns[index].Width.Value;
            }
            else
            {
                fills.Add(index);
            }
        }

        var remaining = Math.Max(0, available - widths.Sum());
        var share = fills.Count == 0 ? 0 : remaining / fills.Count;
        foreach (var index in fills)
        {
            widths[index] = share;
        }

        return widths;
    }

    private static void Rect(StringBuilder output, UiRect bounds, string fill, string stroke, string radius, int depth)
    {
        var indent = new string(' ', depth * 2);
        AppendLine(
            output,
            $"{indent}<rect x=\"{Number(bounds.X)}\" y=\"{Number(bounds.Y)}\" width=\"{Number(bounds.Width)}\" height=\"{Number(bounds.Height)}\" rx=\"{radius}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"1\" />");
    }

    private static void Text(
        StringBuilder output,
        decimal x,
        decimal y,
        string value,
        decimal fontSize,
        string fill,
        string weight,
        int depth)
    {
        var indent = new string(' ', depth * 2);
        AppendLine(
            output,
            $"{indent}<text x=\"{Number(x)}\" y=\"{Number(y)}\" fill=\"{fill}\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"{Number(fontSize)}\" font-weight=\"{weight}\">{Escape(value)}</text>");
    }

    private static string Number(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture);

    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;

    private static void AppendLine(StringBuilder output, string line) => output.Append(line).Append('\n');
}
