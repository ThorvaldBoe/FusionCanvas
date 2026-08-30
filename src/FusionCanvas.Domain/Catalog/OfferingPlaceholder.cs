namespace FusionCanvas.Domain.Catalog;

public sealed record OfferingPlaceholder
{
    private readonly Guid[] _variantIds;

    public OfferingPlaceholder(Guid id, Guid offeringId, string name, string? description, string position, string decorationMethod, int width, int height, IReadOnlyList<Guid> variantIds, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, string metadataJson = "{}", string? providerReference = null, DesignAreaArtworkGuidance? artworkGuidance = null)
    {
        Id = CatalogRecordValidation.Id(id, nameof(id));
        OfferingId = CatalogRecordValidation.Id(offeringId, nameof(offeringId));
        Name = CatalogRecordValidation.Text(name, nameof(name));
        Description = CatalogRecordValidation.Optional(description);
        Position = CatalogRecordValidation.Text(position, nameof(position));
        DecorationMethod = CatalogRecordValidation.Text(decorationMethod, nameof(decorationMethod));
        Width = width > 0 ? width : throw new ArgumentOutOfRangeException(nameof(width), width, "Placeholder width must be positive.");
        Height = height > 0 ? height : throw new ArgumentOutOfRangeException(nameof(height), height, "Placeholder height must be positive.");
        _variantIds = variantIds?.Where(value => value != Guid.Empty).Distinct().ToArray() ?? throw new ArgumentNullException(nameof(variantIds));
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
        ProviderReference = CatalogRecordValidation.Optional(providerReference);
        ArtworkGuidance = artworkGuidance;
    }

    public Guid Id { get; init; }
    public Guid OfferingId { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public string Position { get; init; }
    public string DecorationMethod { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public IReadOnlyList<Guid> VariantIds => _variantIds;
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string MetadataJson { get; init; }
    public string? ProviderReference { get; init; }
    public DesignAreaArtworkGuidance? ArtworkGuidance { get; init; }

    public DesignAreaPhysicalSize? MaximumPhysicalSize => ArtworkGuidance?.PhysicalSizeFor(Width, Height);

    public bool Equals(OfferingPlaceholder? other) => other is not null
        && Id == other.Id && OfferingId == other.OfferingId && Name == other.Name && Description == other.Description
        && Position == other.Position && DecorationMethod == other.DecorationMethod && Width == other.Width && Height == other.Height
        && IsArchived == other.IsArchived && CreatedAt == other.CreatedAt && UpdatedAt == other.UpdatedAt
        && MetadataJson == other.MetadataJson && ProviderReference == other.ProviderReference
        && ArtworkGuidance == other.ArtworkGuidance && _variantIds.SequenceEqual(other._variantIds);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id); hash.Add(OfferingId); hash.Add(Name); hash.Add(Description); hash.Add(Position); hash.Add(DecorationMethod); hash.Add(Width); hash.Add(Height); hash.Add(IsArchived); hash.Add(CreatedAt); hash.Add(UpdatedAt); hash.Add(MetadataJson); hash.Add(ProviderReference); hash.Add(ArtworkGuidance);
        foreach (var value in _variantIds) hash.Add(value);
        return hash.ToHashCode();
    }
}

public static class CatalogRelationshipPolicy
{
    public static void ValidateOffering(
        BlueprintOffering offering,
        Blueprint blueprint,
        PrintProvider? printProvider,
        IReadOnlyList<OfferingOption> options,
        IReadOnlyList<OfferingOptionValue> values,
        IReadOnlyList<OfferingVariant> variants,
        IReadOnlyList<OfferingPlaceholder> placeholders)
    {
        if (offering.BlueprintId != blueprint.Id || offering.StoreId != blueprint.StoreId)
            throw new InvalidOperationException("Offering must belong to its Blueprint and Store.");

        if (offering.Kind == BlueprintOfferingKind.FixedPrintProvider && offering.PrintProviderId is null)
            throw new InvalidOperationException("A fixed-provider offering requires a Print Provider.");
        if (offering.Kind == BlueprintOfferingKind.ProviderNetwork && string.IsNullOrWhiteSpace(offering.ProviderNetworkCode))
            throw new InvalidOperationException("A Provider-Network offering requires a stable provider-network code.");
        if (offering.Kind == BlueprintOfferingKind.FixedPrintProvider && offering.ProviderNetworkCode is not null)
            throw new InvalidOperationException("A fixed-provider offering must not have a Provider-Network code.");
        if (offering.Kind == BlueprintOfferingKind.ProviderNetwork && offering.PrintProviderId is not null)
            throw new InvalidOperationException("A Provider-Network offering must not reference a Print Provider.");
        if (printProvider is not null && printProvider.StoreId != offering.StoreId)
            throw new InvalidOperationException("Print Provider must belong to the offering Store.");

        var offeringOptions = options.Where(option => option.OfferingId == offering.Id).ToArray();
        var optionIds = offeringOptions.Select(option => option.Id).ToHashSet();
        if (offeringOptions.Select(option => option.OptionKind).Distinct().Count() != offeringOptions.Length)
            throw new InvalidOperationException("An offering cannot contain duplicate active option kinds.");

        var offeringValues = values.Where(value => value.OfferingId == offering.Id).ToArray();
        if (offeringValues.Any(value => !optionIds.Contains(value.OptionId)))
            throw new InvalidOperationException("Option Values must belong to an Option in the same offering.");

        var valueIds = offeringValues.Select(value => value.Id).ToHashSet();
        foreach (var variant in variants.Where(variant => variant.OfferingId == offering.Id))
        {
            if (variant.OptionValueIds.Any(valueId => !valueIds.Contains(valueId)))
                throw new InvalidOperationException("Variant Option Values must belong to the same offering.");

            var kinds = variant.OptionValueIds.Select(valueId => offeringValues.Single(value => value.Id == valueId).OptionId).ToArray();
            if (kinds.Distinct().Count() != kinds.Length)
                throw new InvalidOperationException("A concrete Variant cannot contain two values from the same Option.");
        }

        foreach (var placeholder in placeholders.Where(placeholder => placeholder.OfferingId == offering.Id))
        {
            if (placeholder.VariantIds.Any(variantId => variants.All(variant => variant.Id != variantId || variant.OfferingId != offering.Id)))
                throw new InvalidOperationException("Placeholder compatibility must reference concrete Variants from the same offering.");
        }
    }

    public static void ValidateMockupTemplateColor(
        Guid offeringId,
        OfferingOptionValue colorValue,
        OfferingOption colorOption,
        Mockups.MockupTemplate template)
    {
        if (template.BlueprintOfferingId != offeringId || colorValue.OfferingId != offeringId || colorOption.OfferingId != offeringId)
            throw new InvalidOperationException("Mockup template color bindings must belong to the template offering.");
        if (colorOption.OptionKind != OptionKind.Color || colorValue.OptionId != colorOption.Id)
            throw new InvalidOperationException("A mockup template color must reference a Color Option Value.");
    }
}
