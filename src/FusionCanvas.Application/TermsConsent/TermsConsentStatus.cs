namespace FusionCanvas.Application.TermsConsent;

public sealed record TermsConsentStatus(
    bool IsCurrent,
    string Summary,
    string? AcceptedTermsVersion,
    string? AcceptedPolicyVersion,
    DateTimeOffset? AcceptedAtUtc)
{
    public static TermsConsentStatus From(TermsConsentRecord? record)
    {
        if (!TermsConsentPolicy.IsCurrent(record))
        {
            return new(
                false,
                "Terms acknowledgement is required before normal workspace use.",
                record?.FusionCanvasTermsVersion,
                record?.AcknowledgementPolicyVersion,
                record?.AcceptedAtUtc);
        }

        return new(
            true,
            $"Accepted FusionCanvas terms {record!.FusionCanvasTermsVersion} and acknowledgement policy {record.AcknowledgementPolicyVersion} on {record.AcceptedAtUtc.ToUniversalTime():u}.",
            record.FusionCanvasTermsVersion,
            record.AcknowledgementPolicyVersion,
            record.AcceptedAtUtc.ToUniversalTime());
    }
}
