namespace FusionCanvas.Application.RejectedPhrases;

public sealed record RejectedPhraseScope(
    Guid? StoreId,
    Guid? NicheId,
    Guid? GroupId,
    bool WholeWorkspace)
{
    public static RejectedPhraseScope WholeWorkspaceView { get; } = new(null, null, null, true);

    public static RejectedPhraseScope ForStore(Guid storeId) =>
        new(storeId, null, null, false);

    public static RejectedPhraseScope ForNiche(Guid storeId, Guid nicheId) =>
        new(storeId, nicheId, null, false);

    public static RejectedPhraseScope ForGroup(Guid storeId, Guid nicheId, Guid groupId) =>
        new(storeId, nicheId, groupId, false);

    public bool IsSingleStoreNiche =>
        StoreId is not null && NicheId is not null && !WholeWorkspace;
}
