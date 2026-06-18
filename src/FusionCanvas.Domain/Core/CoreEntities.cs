namespace FusionCanvas.Domain.Core;

public sealed record Store : CoreEntity
{
    public Store(Guid id, string name, string? notes = null)
        : base(id, name, notes)
    {
    }
}

public sealed record Niche : CoreEntity, ITopicEntity
{
    public Niche(Guid id, Guid storeId, string name, string? notes = null)
        : base(id, name, notes)
    {
        StoreId = DomainGuards.RequireId(storeId, nameof(storeId));
    }

    public Guid StoreId { get; }
}

public sealed record Group : CoreEntity, ITopicEntity
{
    public Group(Guid id, Guid storeId, Guid? nicheId, Guid? parentGroupId, string name, string? notes = null)
        : base(id, name, notes)
    {
        StoreId = DomainGuards.RequireId(storeId, nameof(storeId));
        NicheId = ValidateParent(nicheId, parentGroupId, nameof(nicheId), nameof(parentGroupId));
        ParentGroupId = parentGroupId;
    }

    public Guid StoreId { get; }

    public Guid? NicheId { get; }

    public Guid? ParentGroupId { get; }

    private static Guid? ValidateParent(Guid? nicheId, Guid? parentGroupId, string nicheParameterName, string groupParameterName)
    {
        if (nicheId is null && parentGroupId is null)
        {
            throw new ArgumentException("A group must belong under a niche or another group.", nicheParameterName);
        }

        if (nicheId is not null && parentGroupId is not null)
        {
            throw new ArgumentException("A group must not have both a niche parent and a group parent.", groupParameterName);
        }

        if (nicheId == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", nicheParameterName);
        }

        if (parentGroupId == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", groupParameterName);
        }

        return nicheId;
    }
}

public sealed record Listing : CoreEntity, IItemEntity
{
    public Listing(Guid id, Guid storeId, Guid? nicheId, Guid? groupId, string name, string? notes = null)
        : base(id, name, notes)
    {
        StoreId = DomainGuards.RequireId(storeId, nameof(storeId));
        NicheId = ValidateOptionalId(nicheId, nameof(nicheId));
        GroupId = ValidateOptionalId(groupId, nameof(groupId));
    }

    public Guid StoreId { get; }

    public Guid? NicheId { get; }

    public Guid? GroupId { get; }

    private static Guid? ValidateOptionalId(Guid? value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", parameterName);
        }

        return value;
    }
}

public sealed record Asset : CoreEntity
{
    public Asset(Guid id, Guid storeId, string resourceReference, Guid? listingId, string name, string? notes = null)
        : base(id, name, notes)
    {
        StoreId = DomainGuards.RequireId(storeId, nameof(storeId));
        ResourceReference = DomainGuards.RequireText(resourceReference, nameof(resourceReference));
        ListingId = ValidateOptionalId(listingId, nameof(listingId));
    }

    public Guid StoreId { get; }

    public string ResourceReference { get; }

    public Guid? ListingId { get; }

    private static Guid? ValidateOptionalId(Guid? value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", parameterName);
        }

        return value;
    }
}

public sealed record Prompt : CoreEntity
{
    public Prompt(
        Guid id,
        Guid storeId,
        string text,
        string name,
        Guid? nicheId = null,
        Guid? groupId = null,
        Guid? listingId = null,
        Guid? assetId = null,
        string? notes = null)
        : base(id, name, notes)
    {
        StoreId = DomainGuards.RequireId(storeId, nameof(storeId));
        Text = DomainGuards.RequireText(text, nameof(text));
        NicheId = ValidateOptionalId(nicheId, nameof(nicheId));
        GroupId = ValidateOptionalId(groupId, nameof(groupId));
        ListingId = ValidateOptionalId(listingId, nameof(listingId));
        AssetId = ValidateOptionalId(assetId, nameof(assetId));
    }

    public Guid StoreId { get; }

    public string Text { get; }

    public Guid? NicheId { get; }

    public Guid? GroupId { get; }

    public Guid? ListingId { get; }

    public Guid? AssetId { get; }

    private static Guid? ValidateOptionalId(Guid? value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", parameterName);
        }

        return value;
    }
}

public sealed record Tag : CoreEntity
{
    public Tag(Guid id, Guid storeId, string name, string? notes = null)
        : base(id, name, notes)
    {
        StoreId = DomainGuards.RequireId(storeId, nameof(storeId));
    }

    public Guid StoreId { get; }
}

public sealed record TagLink(Guid TagId, CoreEntityKind EntityKind, Guid EntityId)
{
    public Guid TagId { get; } = DomainGuards.RequireId(TagId, nameof(TagId));

    public Guid EntityId { get; } = DomainGuards.RequireId(EntityId, nameof(EntityId));
}
