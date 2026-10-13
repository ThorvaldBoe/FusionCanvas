using FusionCanvas.ApiVerification.Sessions;

namespace FusionCanvas.ApiVerification.Printify;

internal sealed record PrintifyRecoveryTarget(string ProductId, bool AlreadyDeleted);

internal static class PrintifyRecoveryEligibility
{
    public static PrintifyRecoveryTarget? Resolve(
        string requestedRunId,
        int configuredShopId,
        PrintifyResourceJournal? journal,
        VerificationSessionReport? report)
    {
        if (!Guid.TryParseExact(requestedRunId, "N", out _)
            || configuredShopId <= 0
            || journal is null && report is null)
            return null;

        var journalOwned = journal is
        {
            SchemaVersion: 1,
            Status: "owned" or "cleanup-pending" or "delete-uncertain" or "deleted"
        } owned
            && owned.RunId == requestedRunId
            && owned.ShopId == configuredShopId
            && owned.ShopName == VerificationConfiguration.RequiredPrintifyDevTestShopName
            && !string.IsNullOrWhiteSpace(owned.ProductId);

        var reportCreated = report?.Operations.LastOrDefault(operation => operation.OperationId == "printify-product-create");
        var reportOwned = report is
        {
            SchemaVersion: 1,
            Provider: "Printify"
        } session
            && session.RunId == requestedRunId
            && reportCreated is not null
            && reportCreated.Status is "passed" or "cleanup-pending" or "uncertain"
            && !string.IsNullOrWhiteSpace(reportCreated.ResourceId);

        if (!journalOwned)
            return null;

        if (report is not null && (!reportOwned || !string.Equals(journal!.ProductId, reportCreated!.ResourceId, StringComparison.Ordinal)))
            return null;

        var productId = journal!.ProductId;
        var deleted = journalOwned && journal!.Status == "deleted"
            || report?.Operations.Any(operation => operation.OperationId == "printify-product-delete" && operation.Status == "passed") == true;
        return deleted ? new(productId!, AlreadyDeleted: true) : new(productId!, AlreadyDeleted: false);
    }
}
