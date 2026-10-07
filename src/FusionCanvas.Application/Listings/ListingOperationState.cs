namespace FusionCanvas.Application.Listings;

public enum ListingOperationState
{
    None = 0,
    IntentRecorded = 1,
    Succeeded = 2,
    Failed = 3,
    NeedsReconciliation = 4
}
