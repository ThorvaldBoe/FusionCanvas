using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Stores;

namespace FusionCanvas.Application.Tests.Stores;

internal sealed class TestStoreContextMapper : IStoreContextMapper
{
    private readonly Dictionary<Guid, StoreContext> _contexts = [];

    public StoreContext Read(Store store) =>
        _contexts.GetValueOrDefault(store.Id, new StoreContext(store.Description));

    public Store Apply(Store store, StoreContext context)
    {
        _contexts[store.Id] = context;
        return store with { Description = context.Description };
    }
}
