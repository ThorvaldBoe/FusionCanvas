using FusionCanvas.Application.AI;

namespace FusionCanvas.ApiVerification.OpenRouter;

internal static class OpenRouterCostBound
{
    private const decimal SafetyFactor = 1.25m;

    public static decimal? EstimateTextMaximum(AiModelEndpointDescriptor endpoint, int promptUtf8Bytes, int maximumCompletionTokens)
    {
        if (promptUtf8Bytes < 0 || maximumCompletionTokens <= 0 || endpoint.PromptPrice is not { } promptPrice || endpoint.CompletionPrice is not { } completionPrice)
        {
            return null;
        }

        if (promptPrice < 0 || completionPrice < 0)
        {
            return null;
        }

        try
        {
            return checked((promptPrice * promptUtf8Bytes + completionPrice * maximumCompletionTokens) * SafetyFactor);
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    public static decimal? EstimateImageMaximum(
        AiImageEndpointCapabilities endpoint,
        AiImageSize size,
        int imageCount,
        int inputTextUtf8Bytes = 0)
    {
        if (size.Width <= 0 || size.Height <= 0 || imageCount <= 0 || inputTextUtf8Bytes < 0 || endpoint.Pricing is not { Count: > 0 } pricing)
        {
            return null;
        }

        try
        {
            decimal total = 0;
            foreach (var price in pricing)
            {
                if (price.CostUsd < 0)
                {
                    return null;
                }

                var quantity = price.Billable.ToLowerInvariant() switch
                {
                    "output_image" when price.Unit.Equals("image", StringComparison.OrdinalIgnoreCase) => imageCount,
                    "output_megapixel" when price.Unit.Equals("megapixel", StringComparison.OrdinalIgnoreCase) =>
                        checked((int)Math.Ceiling((decimal)size.PixelCount / 1_000_000m)) * imageCount,
                    "output_image" when price.Unit.Equals("token", StringComparison.OrdinalIgnoreCase) =>
                        TryGetKnownImageOutputTokens(endpoint.ModelId, size) is { } outputTokens
                            ? checked(outputTokens * imageCount)
                            : -1,
                    "input_text" when price.Unit.Equals("token", StringComparison.OrdinalIgnoreCase) => inputTextUtf8Bytes,
                    "request" when price.Unit.Equals("request", StringComparison.OrdinalIgnoreCase) => 1,
                    "input_image" or "input_reference" or "input_font" => 0,
                    _ => -1
                };

                if (quantity < 0)
                {
                    return null;
                }

                total = checked(total + price.CostUsd * quantity);
            }

            return checked(total * SafetyFactor);
        }
        catch (OverflowException) { return null; }
    }

    private static int? TryGetKnownImageOutputTokens(string modelId, AiImageSize size)
    {
        if (size != new AiImageSize(1024, 1024))
        {
            return null;
        }

        // These 1K square output-token counts are documented by Google's image-generation API.
        // Keep unknown model variants deferred instead of guessing a token bound.
        return modelId.ToLowerInvariant() switch
        {
            "google/gemini-2.5-flash-image" => 1290,
            "google/gemini-3.1-flash-image" or
            "google/gemini-3.1-flash-image-preview" or
            "google/gemini-3.1-flash-lite-image" or
            "google/gemini-nano-banana-2.1" or
            "google/gemini-3-pro-image" or
            "google/gemini-3-pro-image-preview" => 1120,
            _ => null
        };
    }
}
