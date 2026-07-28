namespace FusionCanvas.Domain.Ideation;

public sealed record IdeationRejection
{
    public IdeationRejection(
        Guid id,
        Guid storeId,
        Guid nicheId,
        Guid? groupId,
        string text,
        string? reason,
        IdeationMode mode,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt = null)
    {
        Id = RequireId(id, nameof(id));
        StoreId = RequireId(storeId, nameof(storeId));
        NicheId = RequireId(nicheId, nameof(nicheId));
        GroupId = groupId == Guid.Empty
            ? throw new ArgumentException("Group identifier must not be empty.", nameof(groupId))
            : groupId;
        Text = string.IsNullOrWhiteSpace(text)
            ? throw new ArgumentException("Rejected idea text is required.", nameof(text))
            : text.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Mode = Enum.IsDefined(mode)
            ? mode
            : throw new ArgumentOutOfRangeException(nameof(mode), mode, "Ideation mode is not supported.");
        CreatedAt = createdAt;
        UpdatedAt = updatedAt is { } updatedValue && updatedValue < createdAt
            ? throw new ArgumentException("Updated timestamp must not predate creation.", nameof(updatedAt))
            : updatedAt;
    }

    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public Guid NicheId { get; init; }

    public Guid? GroupId { get; init; }

    public string Text { get; init; }

    public string? Reason { get; init; }

    public IdeationMode Mode { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    private static Guid RequireId(Guid value, string name) =>
        value == Guid.Empty
            ? throw new ArgumentException("Identifier must not be empty.", name)
            : value;
}
