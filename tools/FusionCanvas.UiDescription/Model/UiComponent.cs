namespace FusionCanvas.UiDescription.Model;

public sealed record UiComponent
{
    public required string Id { get; init; }

    public required string Kind { get; init; }

    public string? Variant { get; init; }

    public string? Text { get; init; }

    public UiLength? Width { get; init; }

    public UiLength? Height { get; init; }

    public decimal? MinWidth { get; init; }

    public decimal? MinHeight { get; init; }

    public string? Gap { get; init; }

    public string? Padding { get; init; }

    public string? Axis { get; init; }

    public string? Align { get; init; }

    public IReadOnlyList<UiLength> Columns { get; init; } = [];

    public IReadOnlyList<UiLength> RowTracks { get; init; } = [];

    public int? Column { get; init; }

    public int? Row { get; init; }

    public int ColumnSpan { get; init; } = 1;

    public int RowSpan { get; init; } = 1;

    public IReadOnlyList<UiComponent> Children { get; init; } = [];

    public IReadOnlyList<string> Items { get; init; } = [];

    public IReadOnlyList<UiTableColumn> TableColumns { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<string>> TableRows { get; init; } = [];

    public bool Visible { get; init; } = true;

    public bool Enabled { get; init; } = true;

    public required UiSourceLocation Source { get; init; }
}

public sealed record UiTableColumn(string Header, UiLength Width, UiSourceLocation Source);
