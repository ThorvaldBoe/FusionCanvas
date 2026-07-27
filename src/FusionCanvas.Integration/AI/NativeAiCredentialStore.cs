using FusionCanvas.Application.AI;
using ktsu.CredentialCache;
using ktsu.CredentialCache.Storage;
using ktsu.Semantics.Strings;

namespace FusionCanvas.Integration.AI;

public sealed class NativeAiCredentialStore : IAiCredentialStore
{
    public const string ServiceName = "FusionCanvas";
    private static readonly PersonaGUID OpenRouterPersona =
        SemanticString<PersonaGUID>.Create("openrouter-api-key");

    private readonly ICredentialStore _store;

    public NativeAiCredentialStore()
        : this(CredentialStoreFactory.CreateDefault(ServiceName))
    {
    }

    internal NativeAiCredentialStore(ICredentialStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default) =>
        RunAsync(() =>
        {
            if (!_store.TryLoad(OpenRouterPersona, out var credential))
            {
                return AiCredentialReadResult.NotFound;
            }

            if (credential is not CredentialWithToken token ||
                string.IsNullOrWhiteSpace(token.Token.ToString()))
            {
                return AiCredentialReadResult.Failure(
                    AiCredentialStateKind.InvalidStoredValue,
                    "The saved OpenRouter credential is malformed.");
            }

            return AiCredentialReadResult.Available(token.Token.ToString());
        }, cancellationToken, TranslateReadFailure);

    public Task<AiCredentialOperationResult> SaveAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(AiCredentialOperationResult.Failed("Enter a non-empty OpenRouter API key."));
        }

        return RunAsync(
            () =>
            {
                _store.Save(
                    OpenRouterPersona,
                    new CredentialWithToken
                    {
                        Token = SemanticString<CredentialToken>.Create(apiKey.Trim())
                    });
                return AiCredentialOperationResult.Success;
            },
            cancellationToken,
            exception => AiCredentialOperationResult.Failed(OperationMessage(exception, "saved")));
    }

    public Task<AiCredentialOperationResult> RemoveAsync(CancellationToken cancellationToken = default) =>
        RunAsync(
            () =>
            {
                return _store.Remove(OpenRouterPersona)
                    ? AiCredentialOperationResult.Success
                    : AiCredentialOperationResult.Failed(
                        "The saved OpenRouter key was not found in the native credential store.");
            },
            cancellationToken,
            exception => AiCredentialOperationResult.Failed(OperationMessage(exception, "removed")));

    private static async Task<T> RunAsync<T>(
        Func<T> operation,
        CancellationToken cancellationToken,
        Func<Exception, T> translate)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await Task.Run(operation, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return translate(exception);
        }
    }

    private static AiCredentialReadResult TranslateReadFailure(Exception exception)
    {
        var state = IsDenied(exception)
            ? AiCredentialStateKind.AccessDenied
            : IsLocked(exception)
                ? AiCredentialStateKind.Locked
                : AiCredentialStateKind.Unavailable;
        return AiCredentialReadResult.Failure(state, OperationMessage(exception, "read"));
    }

    private static bool IsDenied(Exception exception) =>
        exception is UnauthorizedAccessException ||
        exception.Message.Contains("denied", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("permission", StringComparison.OrdinalIgnoreCase);

    private static bool IsLocked(Exception exception) =>
        exception.Message.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("keychain", StringComparison.OrdinalIgnoreCase) &&
        exception.Message.Contains("interaction", StringComparison.OrdinalIgnoreCase);

    private static string OperationMessage(Exception exception, string operation)
    {
        if (IsDenied(exception))
        {
            return $"Access to the native credential store was denied; the OpenRouter key was not {operation}.";
        }

        if (IsLocked(exception))
        {
            return $"The native credential store is locked; the OpenRouter key could not be {operation}.";
        }

        return $"The native credential store is unavailable; the OpenRouter key could not be {operation}.";
    }
}
