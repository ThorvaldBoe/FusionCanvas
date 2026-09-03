using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CatalogSetupResult(bool Succeeded, string? Error, CatalogSetupState State, WorkspaceSnapshot? Snapshot = null)
{
    public static CatalogSetupResult Success(CatalogSetupState state) => new(true, null, state);
    public static CatalogSetupResult Failure(string error, CatalogSetupState state) => new(false, error, state);
}
