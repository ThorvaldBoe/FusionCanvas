using FusionCanvas.Domain.Ideation;

namespace FusionCanvas.Application.RejectedPhrases;

public sealed record RejectedPhraseSummary(
    Guid Id,
    Guid StoreId,
    Guid NicheId,
    Guid? GroupId,
    string Text,
    string? Reason,
    IdeationMode Mode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static RejectedPhraseSummary From(IdeationRejection rejection) =>
        new(
            rejection.Id,
            rejection.StoreId,
            rejection.NicheId,
            rejection.GroupId,
            rejection.Text,
            rejection.Reason,
            rejection.Mode,
            rejection.CreatedAt,
            rejection.UpdatedAt);
}
