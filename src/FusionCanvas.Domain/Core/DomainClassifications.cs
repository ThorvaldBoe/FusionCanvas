namespace FusionCanvas.Domain.Core;

public enum CoreEntityKind
{
    Store = 0,
    Niche = 1,
    Group = 2,
    Listing = 3,
    Asset = 4,
    Prompt = 5,
    Tag = 6
}

public interface ITopicEntity
{
    Guid StoreId { get; }
}

public interface IItemEntity
{
    Guid StoreId { get; }
}
