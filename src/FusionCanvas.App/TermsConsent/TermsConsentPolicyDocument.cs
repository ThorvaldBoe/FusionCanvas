using System.Text;
using Avalonia.Platform;

namespace FusionCanvas.App.TermsConsent;

internal static class TermsConsentPolicyDocument
{
    private static readonly Uri ResourceUri = new("avares://FusionCanvas.App/Assets/Legal/FusionCanvasTermsOfUse.md");

    public static string Load()
    {
        using var stream = AssetLoader.Open(ResourceUri);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
