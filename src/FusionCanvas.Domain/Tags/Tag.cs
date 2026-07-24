using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tags;

public sealed record Tag : WorkspaceEntity
{
    public Tag(
        Guid id,
        Guid storeId,
        string name,
        string? description,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson,
        string? color = null)
        : base(id, name, description, isArchived, createdAt, updatedAt, metadataJson)
    {
        StoreId = storeId;
        Color = color;
    }

    public Guid StoreId { get; init; }

    public string? Color
    {
        get => _color;
        init => _color = NormalizeColor(value);
    }

    private readonly string? _color;

    public static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        if (!trimmed.StartsWith('#'))
        {
            throw new ArgumentException("Color must be a hex color starting with '#'.", nameof(color));
        }

        var hex = trimmed[1..];
        if (hex.Length == 3 && IsHexHex(hex))
        {
            return $"#{new string(hex[0], 2)}{new string(hex[1], 2)}{new string(hex[2], 2)}".ToUpperInvariant();
        }

        if (hex.Length == 6 && IsHexHex(hex))
        {
            return $"#{hex.ToUpperInvariant()}";
        }

        throw new ArgumentException("Color must be a valid hex color (#RGB or #RRGGBB).", nameof(color));
    }

    private static bool IsHexHex(string value)
    {
        foreach (var c in value)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            {
                return false;
            }
        }

        return true;
    }
}
