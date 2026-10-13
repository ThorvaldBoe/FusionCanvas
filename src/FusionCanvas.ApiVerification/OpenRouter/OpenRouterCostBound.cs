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

    public static decimal? EstimateImageMaximum(AiImageEndpointCapabilities endpoint, AiImageSize size, int imageCount)
    {
        if (size.Width <= 0 || size.Height <= 0 || imageCount <= 0 || endpoint.Pricing is not { Count: > 0 } pricing)
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
                    "request" when price.Unit.Equals("request", StringComparison.OrdinalIgnoreCase) => 1,
                    "input_image" => 0,
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
}
