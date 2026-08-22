using FusionCanvas.UiDescription.Diagnostics;
using FusionCanvas.UiDescription.Model;

namespace FusionCanvas.UiDescription.Layout;

public sealed class UiLayoutEngine(WireframeTokenProfile? tokens = null)
{
    private readonly WireframeTokenProfile tokens = tokens ?? new WireframeTokenProfile();

    public UiLayoutResult Layout(UiDescriptionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var diagnostics = new List<UiDiagnostic>();
        var viewport = new UiRect(0, 0, document.Screen.ViewportWidth, document.Screen.ViewportHeight);
        var root = Arrange(document.Screen.Root, viewport, diagnostics);

        if (root is not null)
        {
            foreach (var node in Flatten(root))
            {
                if (node.Bounds.X < 0 || node.Bounds.Y < 0 ||
                    node.Bounds.Right > viewport.Right + 0.01m || node.Bounds.Bottom > viewport.Bottom + 0.01m)
                {
                    diagnostics.Add(Error("UIDL200", node.Component, "Arranged bounds exceed the declared viewport."));
                }
            }
        }

        diagnostics.Sort();
        return diagnostics.Count == 0 && root is not null
            ? new UiLayoutResult(root, [])
            : new UiLayoutResult(null, diagnostics);
    }

    public UiSize Measure(UiComponent component, decimal availableWidth, decimal availableHeight)
    {
        if (!component.Visible)
        {
            return new UiSize(0, 0);
        }

        var natural = component.Kind switch
        {
            "text" => tokens.MeasureText(component.Text ?? string.Empty, component.Variant),
            "button" => MeasureControl(component, 24, 38),
            "field" => MeasureControl(component, 160, component.Variant == "multiline" ? 96 : 38),
            "select" => MeasureControl(component, 160, 38),
            "divider" => new UiSize(availableWidth, 1),
            "message" => MeasureControl(component, 180, 48),
            "list" => new UiSize(availableWidth, Math.Max(38, component.Items.Count * 38)),
            "table" => new UiSize(availableWidth, 46 * (component.TableRows.Count + 1)),
            "grid" => MeasureGrid(component, availableWidth, availableHeight),
            "stack" or "panel" => MeasureStack(component, availableWidth, availableHeight),
            _ => new UiSize(0, 0)
        };

        return new UiSize(
            ResolveDesired(component.Width, component.MinWidth, natural.Width, availableWidth),
            ResolveDesired(component.Height, component.MinHeight, natural.Height, availableHeight));
    }

    private UiLayoutNode? Arrange(UiComponent component, UiRect bounds, List<UiDiagnostic> diagnostics)
    {
        if (!component.Visible)
        {
            return null;
        }

        if (bounds.Width < (component.MinWidth ?? 0) || bounds.Height < (component.MinHeight ?? 0))
        {
            diagnostics.Add(Error("UIDL201", component, "Available bounds do not satisfy the component minimum size."));
            return null;
        }

        var children = component.Kind switch
        {
            "stack" or "panel" => ArrangeStack(component, bounds, diagnostics),
            "grid" => ArrangeGrid(component, bounds, diagnostics),
            _ => []
        };

        return new UiLayoutNode(component, Round(bounds), children);
    }

    private IReadOnlyList<UiLayoutNode> ArrangeStack(
        UiComponent component,
        UiRect bounds,
        List<UiDiagnostic> diagnostics)
    {
        var inner = bounds.Deflate(tokens.ResolveSpacing(component.Padding));
        var visible = component.Children.Where(child => child.Visible).ToArray();
        if (visible.Length == 0)
        {
            return [];
        }

        var horizontal = component.Axis == "horizontal";
        var gap = tokens.ResolveSpacing(component.Gap);
        var mainAvailable = (horizontal ? inner.Width : inner.Height) - (gap * (visible.Length - 1));
        var crossAvailable = horizontal ? inner.Height : inner.Width;
        var contentSizes = visible.Select(child => Measure(child, inner.Width, inner.Height)).ToArray();
        var requested = new decimal[visible.Length];
        var fillIndexes = new List<int>();

        for (var index = 0; index < visible.Length; index++)
        {
            var length = horizontal ? visible[index].Width : visible[index].Height;
            if (length?.Kind == UiLengthKind.Fill)
            {
                fillIndexes.Add(index);
                requested[index] = horizontal ? visible[index].MinWidth ?? 0 : visible[index].MinHeight ?? 0;
            }
            else
            {
                requested[index] = horizontal ? contentSizes[index].Width : contentSizes[index].Height;
            }
        }

        var remaining = mainAvailable - requested.Sum();
        if (remaining < -0.01m)
        {
            diagnostics.Add(Error("UIDL202", component, "Stack content exceeds its available main-axis space."));
            return [];
        }

        if (fillIndexes.Count > 0)
        {
            var share = remaining / fillIndexes.Count;
            foreach (var index in fillIndexes)
            {
                requested[index] += share;
            }
        }

        var position = horizontal ? inner.X : inner.Y;
        var result = new List<UiLayoutNode>();
        for (var index = 0; index < visible.Length; index++)
        {
            var child = visible[index];
            var cross = ResolveCross(horizontal ? child.Height : child.Width, horizontal ? child.MinHeight : child.MinWidth, horizontal ? contentSizes[index].Height : contentSizes[index].Width, crossAvailable);
            var crossPosition = AlignCross(component.Align, horizontal ? inner.Y : inner.X, crossAvailable, cross);
            var childBounds = horizontal
                ? new UiRect(position, crossPosition, requested[index], cross)
                : new UiRect(crossPosition, position, cross, requested[index]);
            var arranged = Arrange(child, childBounds, diagnostics);
            if (arranged is not null)
            {
                result.Add(arranged);
            }

            position += requested[index] + gap;
        }

        return result;
    }

    private IReadOnlyList<UiLayoutNode> ArrangeGrid(
        UiComponent component,
        UiRect bounds,
        List<UiDiagnostic> diagnostics)
    {
        var inner = bounds.Deflate(tokens.ResolveSpacing(component.Padding));
        var gap = tokens.ResolveSpacing(component.Gap);
        var rowCount = Math.Max(
            component.RowTracks.Count,
            component.Children.Where(child => child.Visible).Select(child => (child.Row ?? 0) + child.RowSpan).DefaultIfEmpty(0).Max());
        if (rowCount == 0)
        {
            return [];
        }

        var rowTracks = component.RowTracks.Count == 0
            ? Enumerable.Repeat(UiLength.Content, rowCount).ToArray()
            : component.RowTracks.ToArray();

        var columnSizes = ResolveTracks(component.Columns, inner.Width, gap, true, component, diagnostics);
        var rowSizes = ResolveTracks(rowTracks, inner.Height, gap, false, component, diagnostics);
        if (columnSizes is null || rowSizes is null)
        {
            return [];
        }

        var columnOffsets = Offsets(inner.X, columnSizes, gap);
        var rowOffsets = Offsets(inner.Y, rowSizes, gap);
        var result = new List<UiLayoutNode>();
        foreach (var child in component.Children.Where(child => child.Visible))
        {
            var column = child.Column ?? 0;
            var row = child.Row ?? 0;
            if (column >= columnSizes.Length || row >= rowSizes.Length)
            {
                diagnostics.Add(Error("UIDL203", child, "Grid placement cannot be arranged within the declared tracks."));
                continue;
            }

            var width = columnSizes.Skip(column).Take(child.ColumnSpan).Sum() + (gap * (child.ColumnSpan - 1));
            var height = rowSizes.Skip(row).Take(child.RowSpan).Sum() + (gap * (child.RowSpan - 1));
            var cell = new UiRect(columnOffsets[column], rowOffsets[row], width, height);
            var desired = Measure(child, width, height);
            var childWidth = child.Width?.Kind == UiLengthKind.Content ? desired.Width : width;
            var childHeight = child.Height?.Kind == UiLengthKind.Fill ? height : desired.Height;
            childWidth = Math.Min(width, childWidth);
            childHeight = Math.Min(height, childHeight);
            var childBounds = new UiRect(cell.X, cell.Y, childWidth, childHeight);
            var arranged = Arrange(child, childBounds, diagnostics);
            if (arranged is not null)
            {
                result.Add(arranged);
            }
        }

        return result;
    }

    private decimal[]? ResolveTracks(
        IReadOnlyList<UiLength> tracks,
        decimal available,
        decimal gap,
        bool columns,
        UiComponent grid,
        List<UiDiagnostic> diagnostics)
    {
        var sizes = new decimal[tracks.Count];
        var fill = new List<int>();
        for (var index = 0; index < tracks.Count; index++)
        {
            switch (tracks[index].Kind)
            {
                case UiLengthKind.Fixed:
                    sizes[index] = tracks[index].Value;
                    break;
                case UiLengthKind.Fill:
                    fill.Add(index);
                    break;
                default:
                    sizes[index] = MeasureTrackContent(grid, index, columns, available);
                    break;
            }
        }

        var remaining = available - (gap * Math.Max(0, tracks.Count - 1)) - sizes.Sum();
        if (remaining < -0.01m)
        {
            diagnostics.Add(Error("UIDL204", grid, $"Grid {(columns ? "columns" : "rows")} exceed available space."));
            return null;
        }

        if (fill.Count > 0)
        {
            var share = remaining / fill.Count;
            foreach (var index in fill)
            {
                sizes[index] = share;
            }
        }

        return sizes;
    }

    private decimal MeasureTrackContent(UiComponent grid, int track, bool columns, decimal available)
    {
        var matching = grid.Children.Where(child =>
            child.Visible && (columns ? child.Column ?? 0 : child.Row ?? 0) == track &&
            (columns ? child.ColumnSpan : child.RowSpan) == 1);
        return matching
            .Select(child =>
            {
                var size = Measure(child, available, available);
                return columns ? size.Width : size.Height;
            })
            .DefaultIfEmpty(0)
            .Max();
    }

    private UiSize MeasureStack(UiComponent component, decimal availableWidth, decimal availableHeight)
    {
        var padding = tokens.ResolveSpacing(component.Padding);
        var visible = component.Children.Where(child => child.Visible).ToArray();
        var gap = tokens.ResolveSpacing(component.Gap) * Math.Max(0, visible.Length - 1);
        var sizes = visible.Select(child => Measure(child, Math.Max(0, availableWidth - (2 * padding)), availableHeight)).ToArray();
        var horizontal = component.Axis == "horizontal";
        var width = horizontal ? sizes.Sum(size => size.Width) + gap : sizes.Select(size => size.Width).DefaultIfEmpty(0).Max();
        var height = horizontal ? sizes.Select(size => size.Height).DefaultIfEmpty(0).Max() : sizes.Sum(size => size.Height) + gap;
        return new UiSize(width + (2 * padding), height + (2 * padding));
    }

    private UiSize MeasureGrid(UiComponent component, decimal availableWidth, decimal availableHeight)
    {
        var gap = tokens.ResolveSpacing(component.Gap);
        var padding = tokens.ResolveSpacing(component.Padding);
        var width = component.Columns.Sum(track => track.Kind == UiLengthKind.Fixed ? track.Value : 0) +
                    (gap * Math.Max(0, component.Columns.Count - 1));
        var rowCount = Math.Max(
            component.RowTracks.Count,
            component.Children.Where(child => child.Visible).Select(child => (child.Row ?? 0) + child.RowSpan).DefaultIfEmpty(0).Max());
        var height = component.RowTracks.Sum(track => track.Kind == UiLengthKind.Fixed ? track.Value : 0) +
                     (gap * Math.Max(0, rowCount - 1));
        if (component.Columns.Any(track => track.Kind != UiLengthKind.Fixed))
        {
            width = Math.Max(width, availableWidth - (2 * padding));
        }

        if (component.RowTracks.Any(track => track.Kind == UiLengthKind.Fill))
        {
            height = Math.Max(height, availableHeight - (2 * padding));
        }
        else
        {
            for (var row = 0; row < rowCount; row++)
            {
                if (row >= component.RowTracks.Count || component.RowTracks[row].Kind == UiLengthKind.Content)
                {
                    height += MeasureTrackContent(component, row, false, availableHeight);
                }
            }
        }

        return new UiSize(width + (2 * padding), height + (2 * padding));
    }

    private UiSize MeasureControl(UiComponent component, decimal minimumContentWidth, decimal defaultHeight)
    {
        var text = tokens.MeasureText(component.Text ?? string.Empty, component.Kind == "button" ? "emphasis" : "body");
        return new UiSize(Math.Max(minimumContentWidth, text.Width + 24), Math.Max(defaultHeight, component.MinHeight ?? 0));
    }

    private static decimal ResolveDesired(UiLength? length, decimal? minimum, decimal natural, decimal available)
    {
        var value = length switch
        {
            { Kind: UiLengthKind.Fixed } fixedLength => fixedLength.Value,
            { Kind: UiLengthKind.Fill } => available,
            _ => natural
        };
        return Math.Min(available, Math.Max(minimum ?? 0, value));
    }

    private static decimal ResolveCross(UiLength? length, decimal? minimum, decimal natural, decimal available) =>
        length?.Kind switch
        {
            UiLengthKind.Fixed => Math.Min(available, Math.Max(minimum ?? 0, length.Value.Value)),
            UiLengthKind.Content => Math.Min(available, Math.Max(minimum ?? 0, natural)),
            _ => available
        };

    private static decimal AlignCross(string? align, decimal start, decimal available, decimal size) => align switch
    {
        "center" => start + ((available - size) / 2),
        "end" => start + available - size,
        _ => start
    };

    private static decimal[] Offsets(decimal start, IReadOnlyList<decimal> sizes, decimal gap)
    {
        var offsets = new decimal[sizes.Count];
        var current = start;
        for (var index = 0; index < sizes.Count; index++)
        {
            offsets[index] = current;
            current += sizes[index] + gap;
        }

        return offsets;
    }

    private static UiRect Round(UiRect value) => new(
        decimal.Round(value.X, 2, MidpointRounding.AwayFromZero),
        decimal.Round(value.Y, 2, MidpointRounding.AwayFromZero),
        decimal.Round(value.Width, 2, MidpointRounding.AwayFromZero),
        decimal.Round(value.Height, 2, MidpointRounding.AwayFromZero));

    private static IEnumerable<UiLayoutNode> Flatten(UiLayoutNode root)
    {
        yield return root;
        foreach (var child in root.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static UiDiagnostic Error(string code, UiComponent component, string message) =>
        new(code, UiDiagnosticSeverity.Error, component.Source, message, component.Id);
}
