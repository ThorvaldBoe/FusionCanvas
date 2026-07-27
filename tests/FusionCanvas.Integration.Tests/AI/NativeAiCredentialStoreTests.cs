using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;
using ktsu.CredentialCache;
using ktsu.CredentialCache.Storage;

namespace FusionCanvas.Integration.Tests.AI;

public class NativeAiCredentialStoreTests
{
    [Fact]
    public async Task SaveReadRemove_RoundTripsThroughLowLevelStore()
    {
        var backend = new FakeStore();
        var store = new NativeAiCredentialStore(backend);

        Assert.True((await store.SaveAsync("secret", TestContext.Current.CancellationToken)).Succeeded);
        var read = await store.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AiCredentialStateKind.Available, read.State);
        Assert.Equal("secret", read.Secret);
        Assert.True((await store.RemoveAsync(TestContext.Current.CancellationToken)).Succeeded);
        Assert.Equal(AiCredentialStateKind.NotFound, (await store.ReadAsync(TestContext.Current.CancellationToken)).State);
    }

    [Fact]
    public async Task BackendFailure_IsTranslatedWithoutSecretDisclosure()
    {
        var backend = new FakeStore { SaveFailure = new CredentialStoreException("permission denied secret-value") };
        var store = new NativeAiCredentialStore(backend);

        var result = await store.SaveAsync("secret-value", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.DoesNotContain("secret-value", result.Message);
        Assert.Contains("denied", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeStore : ICredentialStore
    {
        private Credential? _credential;
        public string Name => "Fake";
        public Exception? SaveFailure { get; set; }

        public bool TryLoad(PersonaGUID persona, out Credential? credential)
        {
            credential = _credential;
            return credential is not null;
        }

        public void Save(PersonaGUID persona, Credential credential)
        {
            if (SaveFailure is not null) throw SaveFailure;
            _credential = credential;
        }

        public bool Remove(PersonaGUID persona)
        {
            var existed = _credential is not null;
            _credential = null;
            return existed;
        }
    }
}
