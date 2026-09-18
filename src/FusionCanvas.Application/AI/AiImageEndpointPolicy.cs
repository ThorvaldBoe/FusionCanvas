namespace FusionCanvas.Application.AI;

public static class AiImageEndpointPolicy
{
    public sealed record EndpointSelection(AiImageEndpointCapabilities Endpoint, AiImageSize ProviderSize);

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
                endpoint.RasterFormats.Any(IsApprovedRasterFormat) &&
                endpoint.SupportedSizes.Count > 0 &&
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
            .Select((endpoint, index) => new EndpointSelectionCandidate(endpoint, SelectSize(endpoint.SupportedSizes, target), index))
            .OrderBy(candidate => candidate.ProviderSize == target ? 0 : 1)
            .ThenBy(candidate => Math.Abs((decimal)candidate.ProviderSize.Width / candidate.ProviderSize.Height - (decimal)target.Width / target.Height))
            .ThenBy(candidate => candidate.ProviderSize.PixelCount > target.PixelCount ? 1 : 0)
            .ThenBy(candidate => candidate.Index)
            .FirstOrDefault();
        return candidates is null ? null : new EndpointSelection(candidates.Endpoint, candidates.ProviderSize);
    }

    public static bool IsApprovedRasterFormat(string format) =>
        string.Equals(format, "png", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "image/png", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "webp", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "image/webp", StringComparison.OrdinalIgnoreCase);

    private sealed record Candidate(AiImageSize Size, int Index, decimal RatioDistance, bool IsAtOrBelowTarget);
    private sealed record EndpointSelectionCandidate(AiImageEndpointCapabilities Endpoint, AiImageSize ProviderSize, int Index);
}
