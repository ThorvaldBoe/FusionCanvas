using FusionCanvas.Application.Snowclones;

namespace FusionCanvas.Integration.Snowclones;

public sealed class EmbeddedBundledSnowcloneSource : IBundledSnowcloneSource
{
    internal const string ResourceName =
        "FusionCanvas.Integration.Snowclones.Resources.starter-snowclones.csv";

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = typeof(EmbeddedBundledSnowcloneSource).Assembly
            .GetManifestResourceStream(ResourceName);

        return Task.FromResult(
            stream ?? throw new InvalidOperationException(
                $"Bundled snowclone resource '{ResourceName}' is missing."));
    }
}
