using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Integration.Stores.Printify;
using ktsu.CredentialCache;
using ktsu.CredentialCache.Storage;

namespace FusionCanvas.Integration.Tests.Stores.Printify;

public class NativeStorePrintifyCredentialStoreTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NativeRoundTrip_IsScopedAndSafeOnReplacementFailure()
    {
        var backend = new Backend();
        var adapter = new NativeStorePrintifyCredentialStore(backend);
        var a = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var b = new StoreCredentialScope(Guid.NewGuid(), a.StoreId);
        Assert.True((await adapter.SaveAsync(a, "first", Ct)).Succeeded);
        Assert.True((await adapter.SaveAsync(b, "second", Ct)).Succeeded);
        var reload = new NativeStorePrintifyCredentialStore(backend);
        Assert.Equal("first", (await reload.ReadAsync(a, Ct)).Secret);
        Assert.Equal("second", (await reload.ReadAsync(b, Ct)).Secret);
        backend.Fail = true;
        var failed = await adapter.SaveAsync(a, "secret-sentinel", Ct);
        Assert.False(failed.Succeeded);
        Assert.DoesNotContain("secret-sentinel", failed.ToString());
        backend.Fail = false;
        var read = await adapter.ReadAsync(a, Ct);
        Assert.Equal("first", read.Secret);
        Assert.DoesNotContain("first", read.ToString());
        Assert.Equal(PrintifyConfigurationKind.Missing, (await adapter.ReadAsync(a with { StoreId = Guid.NewGuid() }, Ct)).Status.Kind);
        Assert.Equal(PrintifyConfigurationKind.Missing,
            (await new NativeStorePrintifyCredentialStore(new Backend()).ReadAsync(a, Ct)).Status.Kind);
    }

    [Fact]
    public async Task UnavailableAndCancelledReads_AreSafe()
    {
        var adapter = new NativeStorePrintifyCredentialStore(new Backend { Fail = true });
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(PrintifyConfigurationKind.Unavailable, (await adapter.ReadAsync(scope, Ct)).Status.Kind);
        await Assert.ThrowsAsync<OperationCanceledException>(() => adapter.ReadAsync(scope, new CancellationToken(true)));
    }

    private sealed class Backend : ICredentialStore
    {
        private readonly InMemoryCredentialStore _inner = new();
        public bool Fail { get; set; }
        public string Name => "Isolated test backend";
        public bool TryLoad(PersonaGUID persona, out Credential? credential)
        {
            if (Fail) throw new UnauthorizedAccessException("secret-sentinel");
            return _inner.TryLoad(persona, out credential);
        }
        public void Save(PersonaGUID persona, Credential credential)
        {
            if (Fail) throw new IOException("secret-sentinel");
            _inner.Save(persona, credential);
        }
        public bool Remove(PersonaGUID persona) => _inner.Remove(persona);
    }
}
