namespace FusionCanvas.Application.TermsConsent;

public static class TermsConsentPolicy
{
    public const string FusionCanvasTermsVersion = "0.1";
    public const string AcknowledgementPolicyVersion = "2026-10-04";

    public const string PrintifyTermsUrl = "https://printify.com/terms-of-service/";
    public const string PrintifyIpPolicyUrl = "https://printify.com/intellectual-property-policy/";
    public const string ShopifyTermsUrl = "https://www.shopify.com/legal/terms";
    public const string ShopifyAcceptableUsePolicyUrl = "https://www.shopify.com/legal/aup";
    public const string ShopifyApiTermsUrl = "https://www.shopify.com/legal/api-terms";

    public const int RequiredAcknowledgementCount = 4;

    public static bool IsCurrent(TermsConsentRecord? record) =>
        record is not null &&
        string.Equals(record.FusionCanvasTermsVersion, FusionCanvasTermsVersion, StringComparison.Ordinal) &&
        string.Equals(record.AcknowledgementPolicyVersion, AcknowledgementPolicyVersion, StringComparison.Ordinal) &&
        record.AcceptedAtUtc != default;

    public static bool AreAllAcknowledgementsSelected(
        bool fusionCanvasTerms,
        bool printifyTerms,
        bool shopifyTerms,
        bool intellectualPropertyResponsibility) =>
        fusionCanvasTerms && printifyTerms && shopifyTerms && intellectualPropertyResponsibility;

    public static TermsConsentRecord CreateRecord(DateTimeOffset acceptedAtUtc) =>
        new(FusionCanvasTermsVersion, AcknowledgementPolicyVersion, acceptedAtUtc.ToUniversalTime());
}

public sealed record TermsConsentRecord(
    string FusionCanvasTermsVersion,
    string AcknowledgementPolicyVersion,
    DateTimeOffset AcceptedAtUtc);
