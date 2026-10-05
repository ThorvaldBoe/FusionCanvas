using System.Security.Cryptography;
using FusionCanvas.Application.Updates;

namespace FusionCanvas.Integration.Updates;

public sealed class UpdatePackageDownloader : IUpdatePackageDownloader
{
    private const long MaximumInstallerBytes = 250L * 1024 * 1024;
    private readonly HttpClient _httpClient;
    private readonly IInstallerAuthenticityVerifier _authenticityVerifier;
    private readonly string _downloadDirectory;

    public UpdatePackageDownloader(
        HttpClient httpClient,
        IInstallerAuthenticityVerifier authenticityVerifier,
        string? downloadDirectory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authenticityVerifier = authenticityVerifier ?? throw new ArgumentNullException(nameof(authenticityVerifier));
        _downloadDirectory = downloadDirectory ?? Path.Combine(Path.GetTempPath(), "FusionCanvas", "updates");
    }

    public async Task<VerifiedUpdatePackage> DownloadAndVerifyAsync(
        UpdateManifest manifest,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!UpdateManifestValidator.IsSupported(manifest, UpdatePlatform.WindowsX64))
        {
            throw new InvalidOperationException("The update manifest is not supported by this application.");
        }

        Directory.CreateDirectory(_downloadDirectory);
        var temporaryPath = Path.Combine(_downloadDirectory, $"FusionCanvas-{Guid.NewGuid():N}.download");
        var installerPath = Path.Combine(_downloadDirectory, $"FusionCanvas-{manifest.ProductVersion}-Setup.exe");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, manifest.InstallerUri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is > MaximumInstallerBytes)
            {
                throw new InvalidOperationException("The update package is larger than the supported download limit.");
            }

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 128 * 1024,
                useAsync: true))
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = new byte[128 * 1024];
                long received = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    received += read;
                    if (received > MaximumInstallerBytes)
                    {
                        throw new InvalidOperationException("The update package is larger than the supported download limit.");
                    }

                    hash.AppendData(buffer, 0, read);
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    progress?.Report(new UpdateDownloadProgress(received, response.Content.Headers.ContentLength));
                }

                var actualHash = hash.GetHashAndReset();
                var expectedHash = Convert.FromHexString(manifest.Sha256);
                if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
                {
                    throw new InvalidOperationException("The downloaded update failed checksum verification.");
                }
            }

            await _authenticityVerifier.VerifyAsync(temporaryPath, manifest, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, installerPath, overwrite: true);
            return new VerifiedUpdatePackage(manifest, installerPath);
        }
        catch
        {
            TryDelete(temporaryPath);
            TryDelete(installerPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
