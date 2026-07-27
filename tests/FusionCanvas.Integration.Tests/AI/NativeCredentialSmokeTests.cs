using ktsu.CredentialCache;
using ktsu.CredentialCache.Storage;
using ktsu.Semantics.Strings;

namespace FusionCanvas.Integration.Tests.AI;

public class NativeCredentialSmokeTests
{
    [Fact]
    public void NativeStore_RoundTripsOverwritesAndCleansUp()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable("FUSIONCANVAS_NATIVE_CREDENTIAL_SMOKE"),
            "1",
            StringComparison.Ordinal))
        {
            return;
        }

        var store = CredentialStoreFactory.CreateDefault("FusionCanvas.Tests");
        var persona = CredentialCache.CreatePersonaGUID();
        try
        {
            Assert.False(store.TryLoad(persona, out _));
            store.Save(persona, Token("first"));
            AssertToken(store, persona, "first");
            store.Save(persona, Token("second"));
            AssertToken(store, persona, "second");
            Assert.True(store.Remove(persona));
            Assert.False(store.TryLoad(persona, out _));
        }
        finally
        {
            store.Remove(persona);
        }
    }

    private static CredentialWithToken Token(string value) =>
        new() { Token = SemanticString<CredentialToken>.Create(value) };

    private static void AssertToken(ICredentialStore store, PersonaGUID persona, string expected)
    {
        Assert.True(store.TryLoad(persona, out var credential));
        var token = Assert.IsType<CredentialWithToken>(credential);
        Assert.Equal(expected, token.Token.ToString());
    }
}
