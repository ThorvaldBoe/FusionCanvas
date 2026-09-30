using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Stores;

/// <summary>
/// Maps semantic store context to and from the persisted store entity.
/// Implementations belong at the persistence boundary.
/// </summary>
public interface IStoreContextMapper
{
    StoreContext Read(Store store);

    Store Apply(Store store, StoreContext context);
}
