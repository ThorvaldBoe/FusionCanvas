namespace FusionCanvas.Application.Tags;

public sealed record TagManagementDeleteRequest(Guid TagId, bool ConfirmPermanentDeletion);
