namespace FusionCanvas.Domain.Workspace;

public static class ItemDisplayNameFormatter
{
    public static string Format(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var name = item.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        var hex = item.Id.ToString("N").ToLowerInvariant();
        return $"Untitled item · {hex[..8]}";
    }
}
