namespace FusionCanvas.Domain.Products;

public enum ExternalListingOperationState
{
    None = 0, IntentRecorded = 1, Succeeded = 2, Failed = 3, NeedsReconciliation = 4
}
