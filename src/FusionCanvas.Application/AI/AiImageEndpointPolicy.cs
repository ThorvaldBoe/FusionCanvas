namespace FusionCanvas.Application.AI;

public static class AiImageEndpointPolicy
{
    public sealed record EndpointSelection(
        AiImageEndpointCapabilities Endpoint,
        AiImageSize ProviderSize,
        AiImageGenerationOptions Options);

    public static IReadOnlyList<AiImageEndpointCapabilities> CompatibleEndpoints(
        IReadOnlyList<AiImageEndpointCapabilities> endpoints,
        string modelId,
        bool requireZeroDataRetention,
        bool transparentBackground)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.Where(endpoint =>
                string.Equals(endpoint.ModelId, modelId, StringComparison.Ordinal) &&
                endpoint.SupportsImageOutput &&
                HasDimensionCapability(endpoint) &&
                endpoint.RasterFormats.Any(format => transparentBackground ? IsAlphaRasterFormat(format) : IsApprovedRasterFormat(format)) &&
                (!requireZeroDataRetention || endpoint.ZeroDataRetentionCompatible) &&
                (!transparentBackground || endpoint.SupportsTransparency))
            .ToArray();
    }

    public static AiImageSize SelectSize(
        IReadOnlyList<AiImageSize> supportedSizes,
        AiImageSize target)
    {
        ArgumentNullException.ThrowIfNull(supportedSizes);
        if (supportedSizes.Count == 0)
            throw new InvalidOperationException("The image endpoint does not advertise a supported size.");

        var ranked = supportedSizes
            .Select((size, index) => new Candidate(size, index,
                Math.Abs((decimal)size.Width / size.Height - (decimal)target.Width / target.Height),
                size.PixelCount <= target.PixelCount))
            .OrderBy(candidate => candidate.RatioDistance)
            .ThenByDescending(candidate => candidate.IsAtOrBelowTarget)
            .ThenByDescending(candidate => candidate.IsAtOrBelowTarget ? candidate.Size.PixelCount : -candidate.Size.PixelCount)
            .ThenBy(candidate => candidate.Index)
            .First();
        return ranked.Size;
    }

    public static EndpointSelection? SelectEndpoint(
        IReadOnlyList<AiImageEndpointCapabilities> endpoints,
        string modelId,
        bool requireZeroDataRetention,
        bool transparentBackground,
        AiImageSize target)
    {
        var eligible = CompatibleEndpoints(endpoints, modelId, requireZeroDataRetention, transparentBackground);
        var candidates = eligible
            .Select((endpoint, index) => CreateCandidate(endpoint, target, transparentBackground, index))
            .OrderBy(candidate => candidate.UsesExactSize ? 0 : 1)
            .ThenBy(candidate => candidate.RatioDistance)
            .ThenBy(candidate => candidate.ProviderSize.PixelCount > target.PixelCount ? 1 : 0)
            .ThenBy(candidate => candidate.Index)
            .FirstOrDefault();
        return candidates is null ? null : new EndpointSelection(candidates.Endpoint, candidates.ProviderSize, candidates.Options);
    }

    public static bool IsApprovedRasterFormat(string format) =>
        string.Equals(format, "png", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "image/png", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "webp", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "image/webp", StringComparison.OrdinalIgnoreCase);

    private static bool IsAlphaRasterFormat(string format) =>
        string.Equals(format, "png", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "image/png", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "webp", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "image/webp", StringComparison.OrdinalIgnoreCase);

    private static bool HasDimensionCapability(AiImageEndpointCapabilities endpoint) =>
        endpoint.SupportedSizes.Count > 0 ||
        endpoint.Parameters?.AspectRatios.Count > 0 ||
        endpoint.Parameters?.Resolutions.Count > 0;

    private static EndpointSelectionCandidate CreateCandidate(
        AiImageEndpointCapabilities endpoint,
        AiImageSize target,
        bool transparentBackground,
        int index)
    {
        var parameters = endpoint.Parameters;
        var selectedSize = endpoint.SupportedSizes.Count > 0 ? SelectSize(endpoint.SupportedSizes, target) : (AiImageSize?)null;
        var aspectRatio = selectedSize is null ? SelectAspectRatio(parameters?.AspectRatios ?? [], target) : null;
        var resolution = selectedSize is null ? SelectResolution(parameters?.Resolutions ?? [], target) : null;
        var providerSize = selectedSize ?? EstimateProviderSize(target, aspectRatio, resolution);
        var outputFormat = PreferredRasterFormat(endpoint.RasterFormats, transparentBackground);
        var background = parameters?.Backgrounds.FirstOrDefault(value =>
            string.Equals(value, transparentBackground ? "transparent" : "opaque", StringComparison.OrdinalIgnoreCase));
        var options = new AiImageGenerationOptions(
            Size: selectedSize is not null && (parameters is null || parameters.SupportsExplicitSize) ? selectedSize : null,
            Resolution: resolution,
            AspectRatio: aspectRatio,
            OutputFormat: parameters is null || parameters.SupportsOutputFormat ? outputFormat : null,
            Background: parameters is null ? (transparentBackground ? "transparent" : "opaque") : background,
            Count: parameters is null || parameters.SupportsImageCount ? 1 : null);
        var ratioDistance = selectedSize is not null
            ? RatioDistance(selectedSize.Value.Width, selectedSize.Value.Height, target)
            : RatioDistance(aspectRatio, target);
        return new EndpointSelectionCandidate(
            endpoint,
            providerSize,
            options,
            selectedSize == target,
            ratioDistance,
            index);
    }

    private static string? SelectAspectRatio(IReadOnlyList<string> supportedRatios, AiImageSize target)
    {
        var candidates = supportedRatios
            .Select((value, index) => new { Value = value, Index = index, Ratio = ParseRatio(value) })
            .Where(candidate => candidate.Ratio is not null)
            .OrderBy(candidate => Math.Abs(candidate.Ratio!.Value - (decimal)target.Width / target.Height))
            .ThenBy(candidate => candidate.Index)
            .ToArray();
        if (candidates.Length > 0)
            return candidates[0].Value;
        return supportedRatios.FirstOrDefault(value => string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase));
    }

    private static string? SelectResolution(IReadOnlyList<string> supportedResolutions, AiImageSize target)
    {
        var targetExtent = Math.Max(target.Width, target.Height);
        var candidates = supportedResolutions
            .Select((value, index) => new { Value = value, Index = index, Extent = ParseResolution(value) })
            .Where(candidate => candidate.Extent is not null)
            .ToArray();
        var atOrBelow = candidates
            .Where(candidate => candidate.Extent <= targetExtent)
            .OrderByDescending(candidate => candidate.Extent)
            .ThenBy(candidate => candidate.Index)
            .FirstOrDefault();
        return atOrBelow?.Value ?? candidates.OrderBy(candidate => candidate.Extent).ThenBy(candidate => candidate.Index).FirstOrDefault()?.Value;
    }

    private static AiImageSize EstimateProviderSize(AiImageSize target, string? aspectRatio, string? resolution)
    {
        var ratio = ParseRatio(aspectRatio);
        var extent = ParseResolution(resolution);
        if (ratio is null || extent is null)
            return target;
        var width = ratio >= 1 ? extent.Value : Math.Max(1, (int)Math.Round(extent.Value * ratio.Value, MidpointRounding.AwayFromZero));
        var height = ratio >= 1 ? Math.Max(1, (int)Math.Round(extent.Value / ratio.Value, MidpointRounding.AwayFromZero)) : extent.Value;
        return new AiImageSize(width, height);
    }

    private static decimal RatioDistance(string? ratio, AiImageSize target)
    {
        var parsed = ParseRatio(ratio);
        return parsed is null ? decimal.MaxValue : Math.Abs(parsed.Value - (decimal)target.Width / target.Height);
    }

    private static decimal RatioDistance(int width, int height, AiImageSize target) =>
        Math.Abs((decimal)width / height - (decimal)target.Width / target.Height);

    private static decimal? ParseRatio(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 && decimal.TryParse(parts[0], out var width) && decimal.TryParse(parts[1], out var height) && width > 0 && height > 0
            ? width / height
            : null;
    }

    private static int? ParseResolution(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.EndsWith('K') && decimal.TryParse(normalized[..^1], out var thousands) && thousands > 0)
            return (int)Math.Round(thousands * 1024, MidpointRounding.AwayFromZero);
        return int.TryParse(normalized, out var pixels) && pixels > 0 ? pixels : null;
    }

    private static string PreferredRasterFormat(IReadOnlyList<string> formats, bool transparentBackground)
    {
        var approved = formats.Where(format => transparentBackground ? IsAlphaRasterFormat(format) : IsApprovedRasterFormat(format)).ToArray();
        return approved.FirstOrDefault(format => string.Equals(format, "png", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "image/png", StringComparison.OrdinalIgnoreCase))
            ?? approved[0];
    }

    private sealed record Candidate(AiImageSize Size, int Index, decimal RatioDistance, bool IsAtOrBelowTarget);
    private sealed record EndpointSelectionCandidate(
        AiImageEndpointCapabilities Endpoint,
        AiImageSize ProviderSize,
        AiImageGenerationOptions Options,
        bool UsesExactSize,
        decimal RatioDistance,
        int Index);
}
