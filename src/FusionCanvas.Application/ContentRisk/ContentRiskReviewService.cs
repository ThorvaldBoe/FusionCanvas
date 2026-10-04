using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.ContentRisk;

public sealed class ContentRiskReviewService : IContentRiskReviewService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IContentRiskAnalyzer _analyzer;
    private readonly Func<DateTimeOffset> _clock;

    public ContentRiskReviewService(
        IWorkspaceRepository repository,
        IContentRiskAnalyzer analyzer,
        Func<DateTimeOffset>? clock = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public Task<ContentRiskReview> ReviewTextAsync(
        ContentRiskReviewTarget target,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return ReviewAsync(
            new ContentRiskAnalysisRequest(target, text: text),
            ContentRiskFingerprint.ForText(target, text),
            cancellationToken);
    }

    public Task<ContentRiskReview> ReviewImageAsync(
        ContentRiskReviewTarget target,
        string mediaType,
        byte[] imageBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return ReviewAsync(
            new ContentRiskAnalysisRequest(target, mediaType: mediaType, imageBytes: imageBytes),
            ContentRiskFingerprint.ForImage(target, imageBytes),
            cancellationToken);
    }

    public async Task<ContentRiskReview> EnsureUnreviewedAsync(
        ContentRiskReviewTarget target,
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        var review = ContentRiskReview.Unreviewed(target, fingerprint);
        await PersistAsync(review, cancellationToken).ConfigureAwait(false);
        return review;
    }

    public async Task<ContentRiskReview?> FindAsync(
        ContentRiskReviewTarget target,
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.ContentRiskReviews.FirstOrDefault(review =>
            review.Target == target && review.IsCurrent(fingerprint));
    }

    private async Task<ContentRiskReview> ReviewAsync(
        ContentRiskAnalysisRequest request,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var unreviewed = ContentRiskReview.Unreviewed(request.Target, fingerprint);
        await PersistAsync(unreviewed, cancellationToken).ConfigureAwait(false);

        ContentRiskAnalysisResult result;
        try
        {
            result = await _analyzer.AnalyzeAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = ContentRiskAnalysisResult.Unavailable(
                ContentRiskAnalyzerFailureKind.Cancelled,
                "Content-risk review was cancelled.");
        }
        catch (Exception)
        {
            result = ContentRiskAnalysisResult.Unavailable(
                ContentRiskAnalyzerFailureKind.ProviderFailure,
                "Content-risk review could not be completed.");
        }

        var review = result.Succeeded
            ? BuildSuccessfulReview(request.Target, fingerprint, result)
            : ContentRiskReview.ReviewUnavailable(request.Target, fingerprint);
        await PersistAsync(review, CancellationToken.None).ConfigureAwait(false);
        return review;
    }

    private ContentRiskReview BuildSuccessfulReview(
        ContentRiskReviewTarget target,
        string fingerprint,
        ContentRiskAnalysisResult result)
    {
        var provenance = new ContentRiskReviewProvenance(
            result.AnalyzerId!,
            _clock(),
            result.InputUnits,
            result.ReportedCost);
        return result.Findings.Count == 0
            ? ContentRiskReview.NoObviousSignalDetected(target, fingerprint, provenance)
            : ContentRiskReview.PotentialRisk(target, fingerprint, result.Findings, provenance);
    }

    private async Task PersistAsync(ContentRiskReview review, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var reviews = snapshot.ContentRiskReviews
            .Where(existing => existing.Target != review.Target)
            .Append(review)
            .ToArray();
        await _repository.SaveAsync(snapshot with { ContentRiskReviews = reviews }, cancellationToken).ConfigureAwait(false);
    }
}
