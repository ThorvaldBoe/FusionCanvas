namespace FusionCanvas.Domain.ContentRisk;

public sealed record ContentRiskReviewTarget
{
    public ContentRiskReviewTarget(
        Guid ownerId,
        ContentRiskOwnerKind ownerKind,
        ContentRiskContentKind contentKind,
        string role)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("The content-risk owner identifier must not be empty.", nameof(ownerId));
        }

        if (!Enum.IsDefined(ownerKind))
        {
            throw new ArgumentOutOfRangeException(nameof(ownerKind));
        }

        if (!Enum.IsDefined(contentKind))
        {
            throw new ArgumentOutOfRangeException(nameof(contentKind));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("The content-risk target role is required.", nameof(role));
        }

        OwnerId = ownerId;
        OwnerKind = ownerKind;
        ContentKind = contentKind;
        Role = role.Trim();
    }

    public Guid OwnerId { get; }

    public ContentRiskOwnerKind OwnerKind { get; }

    public ContentRiskContentKind ContentKind { get; }

    public string Role { get; }
}
