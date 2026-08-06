using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;
using ktsu.CredentialCache;
using ktsu.CredentialCache.Storage;
using ktsu.Semantics.Strings;

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

    [Fact]
    public async Task FailedReplacement_PreservesPreviousCredential()
    {
        var backend = new FakeStore();
        var store = new NativeAiCredentialStore(backend);

        Assert.True((await store.SaveAsync("first", TestContext.Current.CancellationToken)).Succeeded);
        backend.SaveFailure = new IOException("vault unavailable");

        var replacement = await store.SaveAsync("second", TestContext.Current.CancellationToken);
        var read = await store.ReadAsync(TestContext.Current.CancellationToken);

        Assert.False(replacement.Succeeded);
        Assert.Equal("first", read.Secret);
    }

    [Fact]
    public async Task ReadAndRemove_TranslateMalformedAndUnavailableStates()
    {
        var backend = new FakeStore
        {
            Credential = new CredentialWithToken
            {
                Token = SemanticString<CredentialToken>.Create(string.Empty)
            }
        };
        var store = new NativeAiCredentialStore(backend);

        Assert.Equal(AiCredentialStateKind.InvalidStoredValue,
            (await store.ReadAsync(TestContext.Current.CancellationToken)).State);

        backend.Credential = null;
        backend.LoadFailure = new Exception("credential store locked");
        Assert.Equal(AiCredentialStateKind.Locked,
            (await store.ReadAsync(TestContext.Current.CancellationToken)).State);

        backend.LoadFailure = null;
        backend.RemoveFailure = new UnauthorizedAccessException();
        var removed = await store.RemoveAsync(TestContext.Current.CancellationToken);
        Assert.False(removed.Succeeded);
    }

    [Fact]
    public async Task Cancellation_IsPropagatedBeforeBackendAccess()
    {
        var backend = new FakeStore();
        var store = new NativeAiCredentialStore(backend);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            store.ReadAsync(new CancellationToken(canceled: true)));
        Assert.False(backend.Accessed);
    }

    private sealed class FakeStore : ICredentialStore
    {
        private Credential? _credential;
        public string Name => "Fake";
        public Exception? SaveFailure { get; set; }
        public Exception? LoadFailure { get; set; }
        public Exception? RemoveFailure { get; set; }
        public Credential? Credential { get => _credential; set => _credential = value; }
        public bool Accessed { get; private set; }

        public bool TryLoad(PersonaGUID persona, out Credential? credential)
        {
            Accessed = true;
            if (LoadFailure is not null) throw LoadFailure;
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
            Accessed = true;
            if (RemoveFailure is not null) throw RemoveFailure;
            var existed = _credential is not null;
            _credential = null;
            return existed;
        }
    }
}
