using FusionCanvas.Application.Stores.Printify;
using ktsu.CredentialCache;
using ktsu.CredentialCache.Storage;
using ktsu.Semantics.Strings;

namespace FusionCanvas.Integration.Stores.Printify;

public sealed class NativeStorePrintifyCredentialStore : IStorePrintifyCredentialStore
{
    private readonly Func<ICredentialStore> _backend;

    public NativeStorePrintifyCredentialStore() : this(() =>
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException();
        return CredentialStoreFactory.CreateDefault("FusionCanvas");
    }) { }

    internal NativeStorePrintifyCredentialStore(ICredentialStore backend) : this(() => backend) { }

    private NativeStorePrintifyCredentialStore(Func<ICredentialStore> backend) => _backend = backend;

    public async Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await Task.Run(() =>
            {
                if (!_backend().TryLoad(Persona(scope), out var credential))
                    return new PrintifyCredentialReadResult(new(PrintifyConfigurationKind.Missing, "Printify api key is required"));
                if (credential is not CredentialWithToken token || !PrintifyToken.IsValid(token.Token.ToString()))
                    return new PrintifyCredentialReadResult(PrintifyConfigurationResult.Unavailable);
                return new PrintifyCredentialReadResult(new(PrintifyConfigurationKind.Available, "Printify api key is provided"), token.Token.ToString());
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return new(PrintifyConfigurationResult.Unavailable); }
    }

    public async Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!PrintifyToken.IsValid(key))
            return new(PrintifyConfigurationKind.InvalidKey, "Enter a non-empty Printify key without control characters.");
        try
        {
            return await Task.Run(() =>
            {
                _backend().Save(Persona(scope), new CredentialWithToken
                {
                    Token = SemanticString<CredentialToken>.Create(key.Trim())
                });
                return new PrintifyConfigurationResult(PrintifyConfigurationKind.Saved, "Printify key saved, not verified.");
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return PrintifyConfigurationResult.Unavailable; }
    }

    private static PersonaGUID Persona(StoreCredentialScope scope)
    {
        if (scope.WorkspaceId == Guid.Empty || scope.StoreId == Guid.Empty) throw new ArgumentException("A saved Store identity is required.");
        return SemanticString<PersonaGUID>.Create($"printify-api-key/{scope.WorkspaceId:D}/{scope.StoreId:D}");
    }
}
