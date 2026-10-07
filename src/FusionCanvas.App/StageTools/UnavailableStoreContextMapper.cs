using FusionCanvas.Application.Stores;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.App.StageTools;

public sealed class UnavailableStoreContextMapper : IStoreContextMapper
{
    public StoreContext Read(Store store) => new(store.Description);

    public Store Apply(Store store, StoreContext context) => store;
}
