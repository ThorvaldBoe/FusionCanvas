using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.Integration.Testing;

public sealed class MockPrintifyCredentialVerifier : IPrintifyCredentialVerifier
{
    public MockPrintifyCredentialVerifier()
    {
        Result = new(
            PrintifyConfigurationKind.Verified,
            "Synthetic Printify key verified.",
            [new(9001, "Synthetic Shop")]);
    }

    public PrintifyConfigurationResult Result { get; set; }

    public int VerificationCalls { get; private set; }

    public Task<PrintifyConfigurationResult> VerifyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        VerificationCalls++;
        return Task.FromResult(PrintifyToken.IsValid(key)
            ? Result
            : new PrintifyConfigurationResult(PrintifyConfigurationKind.InvalidKey, "The synthetic Printify key is invalid."));
    }
}
