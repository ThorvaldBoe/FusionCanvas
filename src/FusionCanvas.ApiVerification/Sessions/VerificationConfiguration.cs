namespace FusionCanvas.ApiVerification.Sessions;

internal sealed record VerificationConfiguration(
    string? OpenRouterApiKey,
    string? PrintifyApiKey,
    string PrintifyDevTestShopName,
    decimal? OpenRouterSessionCostCeiling,
    string DataDirectory)
{
    public const string RequiredPrintifyDevTestShopName = "FusionCanvas DevTest";

    public static VerificationConfiguration FromEnvironment()
    {
        var ceilingText = Environment.GetEnvironmentVariable("FUSIONCANVAS_OPENROUTER_SESSION_MAX_COST_USD");
        decimal? ceiling = decimal.TryParse(ceilingText, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var parsedCeiling) && parsedCeiling > 0
            ? parsedCeiling
            : null;
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localData, "FusionCanvas", "ApiVerification");
        return new(
            Environment.GetEnvironmentVariable("FUSIONCANVAS_OPENROUTER_TEST_API_KEY"),
            Environment.GetEnvironmentVariable("FUSIONCANVAS_PRINTIFY_DEVTEST_API_KEY"),
            RequiredPrintifyDevTestShopName,
            ceiling,
            directory);
    }
}
