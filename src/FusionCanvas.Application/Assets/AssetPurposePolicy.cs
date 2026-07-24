using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Assets;

public static class AssetPurposePolicy
{
    private static readonly IReadOnlyDictionary<string, AssetKind> ExtensionMappings = new Dictionary<string, AssetKind>(StringComparer.OrdinalIgnoreCase)
    {
        [".afdesign"] = AssetKind.SourceDesign,
        [".psd"] = AssetKind.SourceDesign,
        [".ai"] = AssetKind.SourceDesign,
        [".eps"] = AssetKind.SourceDesign,
        [".png"] = AssetKind.ExportedImage,
        [".jpg"] = AssetKind.ExportedImage,
        [".jpeg"] = AssetKind.ExportedImage,
        [".webp"] = AssetKind.ExportedImage,
        [".tif"] = AssetKind.ExportedImage,
        [".tiff"] = AssetKind.ExportedImage,
        [".svg"] = AssetKind.Svg,
        [".ttf"] = AssetKind.Font,
        [".otf"] = AssetKind.Font,
        [".brush"] = AssetKind.Brush
    };

    public static IReadOnlyList<AssetKind> AvailablePurposes { get; } =
    [
        AssetKind.SourceDesign,
        AssetKind.ExportedImage,
        AssetKind.Svg,
        AssetKind.MockupImage,
        AssetKind.ReferenceImage,
        AssetKind.Texture,
        AssetKind.Brush,
        AssetKind.Font,
        AssetKind.Unknown,
        AssetKind.Other
    ];

    public static AssetKind SuggestKind(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return ExtensionMappings.TryGetValue(extension, out var kind) ? kind : AssetKind.Unknown;
    }
}
