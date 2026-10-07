using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.StageTools;

public sealed class WorkspaceListingProjectionSource(
    IWorkspaceFileStore fileStore,
    IRasterImageMetadataReader metadataReader) : IListingProjectionSource
{
    public async Task<ListingProjectionResult> BuildAsync(
        WorkspaceSnapshot snapshot,
        Guid itemId,
        ListingPricingInput pricing,
        string? shippingProfile,
        string? outOfStockPolicy,
        CancellationToken cancellationToken = default)
    {
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId);
        if (item is null)
            return Invalid("item-missing", "The selected Item is no longer available.");

        var configuration = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId);
        var offering = configuration is null
            ? null
            : snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == configuration.OfferingId && !value.IsArchived);
        var blueprint = offering is null
            ? null
            : snapshot.Blueprints.SingleOrDefault(value => value.Id == offering.BlueprintId && !value.IsArchived);
        var provider = offering?.PrintProviderId is Guid providerId
            ? snapshot.PrintProviders.SingleOrDefault(value => value.Id == providerId && !value.IsArchived)
            : null;

        if (offering is null || blueprint is null || provider is null)
            return Invalid("catalog-incomplete", "Select an active Blueprint, fixed Print Provider, and offering in Design before creating a listing.");

        var variants = snapshot.OfferingVariants
            .Where(value => value.OfferingId == offering.Id && !value.IsArchived)
            .ToArray();
        var options = snapshot.OfferingOptions.Where(value => !value.IsArchived).ToArray();
        var optionValues = snapshot.OfferingOptionValues.Where(value => !value.IsArchived).ToArray();
        var areas = BuildDesignAreas(snapshot, offering.Id);
        var assignments = snapshot.DesignSlotAssignments
            .Where(value => areas.Any(area => area.Id == value.DesignAreaId))
            .ToArray();
        var assets = snapshot.Assets.Where(value => !value.IsArchived).ToArray();
        var dimensions = new Dictionary<Guid, ArtworkDimensions>();
        foreach (var asset in assets.Where(value => assignments.Any(assignment => assignment.AssetId == value.Id)))
        {
            if (asset.IsMissing || !fileStore.Exists(asset.WorkspaceRelativePath))
                continue;

            try
            {
                var info = await metadataReader.ReadAsync(fileStore.ResolvePath(asset.WorkspaceRelativePath), cancellationToken).ConfigureAwait(false);
                dimensions[asset.Id] = new ArtworkDimensions(info.Width, info.Height);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The projection builder reports the missing dimensions as a readiness issue.
            }
        }

        return ListingProjectionBuilder.Build(new ListingProjectionRequest(
            item,
            blueprint,
            offering,
            provider,
            variants,
            options,
            optionValues,
            snapshot.DesignSelectedColors,
            snapshot.DesignVariantRows.Where(value => value.ItemId == itemId).ToArray(),
            snapshot.DesignVariantRowColors,
            assignments,
            areas,
            assets,
            dimensions,
            pricing,
            shippingProfile,
            outOfStockPolicy));
    }

    private static IReadOnlyList<DesignArea> BuildDesignAreas(WorkspaceSnapshot snapshot, Guid offeringId)
    {
        var persisted = snapshot.DesignAreas.Where(value => value.FulfillmentOfferingId == offeringId).ToArray();
        if (persisted.Length > 0)
            return persisted;

        return snapshot.OfferingPlaceholders
            .Where(value => value.OfferingId == offeringId && !value.IsArchived)
            .Select(value => new DesignArea(
                value.Id,
                value.OfferingId,
                value.Name,
                value.Description,
                value.Position,
                value.DecorationMethod,
                value.Width,
                value.Height,
                value.VariantIds,
                value.CreatedAt,
                value.UpdatedAt,
                value.MetadataJson))
            .ToArray();
    }

    private static ListingProjectionResult Invalid(string code, string message) =>
        new(null, [new ListingReadinessIssue(code, message)]);
}
