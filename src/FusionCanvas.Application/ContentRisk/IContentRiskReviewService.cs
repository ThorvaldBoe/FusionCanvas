using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.ContentRisk;

public interface IContentRiskReviewService
{
    Task<ContentRiskReview> ReviewTextAsync(
        ContentRiskReviewTarget target,
        string text,
        CancellationToken cancellationToken = default);

    Task<ContentRiskReview> ReviewImageAsync(
        ContentRiskReviewTarget target,
        string mediaType,
        byte[] imageBytes,
        CancellationToken cancellationToken = default);

    Task<ContentRiskReview> EnsureUnreviewedAsync(
        ContentRiskReviewTarget target,
        string fingerprint,
        CancellationToken cancellationToken = default);

    Task<ContentRiskReview?> FindAsync(
        ContentRiskReviewTarget target,
        string fingerprint,
        CancellationToken cancellationToken = default);
}
