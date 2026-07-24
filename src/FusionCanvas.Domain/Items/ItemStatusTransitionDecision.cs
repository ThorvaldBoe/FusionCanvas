namespace FusionCanvas.Domain.Items;

public sealed record ItemStatusTransitionDecision(bool IsAllowed, bool RequiresConfirmation, string Reason);
