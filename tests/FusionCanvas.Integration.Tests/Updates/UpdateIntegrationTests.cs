using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using FusionCanvas.Application.Updates;
using FusionCanvas.Integration.Updates;

namespace FusionCanvas.Integration.Tests.Updates;

public sealed class UpdateIntegrationTests
{
    [Fact]
    public async Task GitHubUpdateSource_ListsStableAndPreviewReleaseAssets()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"tag_name":"v0.4.0-preview.1","prerelease":true,"draft":false,"html_url":"https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.4.0-preview.1","assets":[{"name":"FusionCanvas-0.4.0-preview.1.json"}]},{"tag_name":"v0.3.0","prerelease":false,"draft":false,"html_url":"https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0","assets":[{"name":"FusionCanvas-0.3.0.json"}]}]""", Encoding.UTF8, "application/json")
        });

        var releases = await new GitHubUpdateSource(client).GetReleasesAsync();

        Assert.Equal(2, releases.Count);
        Assert.Equal(UpdateTrack.Preview, releases[0].Track);
        Assert.Equal("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0.json", releases[1].ManifestUri.AbsoluteUri);
    }

    [Fact]
    public async Task GitHubUpdateSource_RejectsMalformedReleaseData()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json", Encoding.UTF8, "application/json")
        });
        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() => new GitHubUpdateSource(client).GetReleasesAsync());
    }

    [Fact]
    public async Task GitHubUpdateSource_StopsAtReleaseHistoryLimit()
    {
        var item = """{"tag_name":"v0.3.0","prerelease":false,"draft":false,"html_url":"https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0","assets":[{"name":"FusionCanvas-0.3.0.json"}]}""";
        var fullPage = "[" + string.Join(',', Enumerable.Repeat(item, 100)) + "]";
        var requests = 0;
        using var client = CreateClient(_ => { requests++; return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(fullPage, Encoding.UTF8, "application/json") }; });

        await Assert.ThrowsAsync<InvalidOperationException>(() => new GitHubUpdateSource(client).GetReleasesAsync());
        Assert.Equal(5, requests);
    }

    [Fact]
    public async Task GitHubUpdateSource_DeserializesVersionedManifestUsingCanonicalUrl()
    {
        const string json = """{"schemaVersion":3,"productVersion":"0.3.0","platform":"win-x64","track":"stable","tagName":"v0.3.0","sourceCommit":"CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC","installerUri":"https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe","sha256":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA","releaseUri":"https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0","bundleUri":"https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe.sigstore.json"}""";
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        var release = new UpdateRelease("v0.3.0", "0.3.0", UpdateTrack.Stable,
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"),
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0.json"));

        var manifest = await new GitHubUpdateSource(client).GetManifestAsync(release);

        Assert.NotNull(manifest);
        Assert.Equal(UpdateTrack.Stable, manifest.Track);
        Assert.Equal(new string('C', 40), manifest.SourceCommit);
    }

    [Fact]
    public async Task GitHubUpdateSource_RejectsNoncanonicalManifestAssetBeforeRequest()
    {
        using var client = CreateClient(_ => throw new InvalidOperationException("The source must not request an untrusted URL."));
        var release = new UpdateRelease("v0.3.0", "0.3.0", UpdateTrack.Stable,
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"), new Uri("https://example.test/manifest.json"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => new GitHubUpdateSource(client).GetManifestAsync(release));
    }

    [Fact]
    public async Task GitHubUpdateSource_PropagatesCancellationAndNetworkErrors()
    {
        using (var client = new HttpClient(new BlockingHandler()))
        using (var cancellation = new CancellationTokenSource())
        {
            var request = new GitHubUpdateSource(client).GetReleasesAsync(cancellation.Token);
            await BlockingHandler.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        }
        using var failedClient = CreateClient(_ => throw new HttpRequestException("offline"));
        await Assert.ThrowsAsync<HttpRequestException>(() => new GitHubUpdateSource(failedClient).GetReleasesAsync());
    }

    [Fact]
    public async Task Downloader_VerifiesChecksumBeforeReturningPackage()
    {
        var bytes = Encoding.UTF8.GetBytes("installer bytes");
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var manifest = Manifest(hash);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        });
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvasUpdateTests", Guid.NewGuid().ToString("N"));

        try
        {
            var package = await new UpdatePackageDownloader(client, new AcceptingAuthenticityVerifier(), directory).DownloadAndVerifyAsync(manifest);

            Assert.True(File.Exists(package.InstallerPath));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(package.InstallerPath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Downloader_RejectsChecksumMismatch()
    {
        var manifest = Manifest(new string('A', 64));
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("wrong bytes"))
        });
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvasUpdateTests", Guid.NewGuid().ToString("N"));

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new UpdatePackageDownloader(
                    httpClient: client,
                    authenticityVerifier: new AcceptingAuthenticityVerifier(),
                    downloadDirectory: directory)
                    .DownloadAndVerifyAsync(manifest));
            Assert.Empty(Directory.EnumerateFiles(directory));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Downloader_CancellationCleansUpPartialFile()
    {
        var manifest = Manifest(new string('A', 64));
        var contentStream = new PartiallyBlockingStream();
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(contentStream)
        });
        using var cancellation = new CancellationTokenSource();
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvasUpdateTests", Guid.NewGuid().ToString("N"));

        try
        {
            var download = new UpdatePackageDownloader(client, new AcceptingAuthenticityVerifier(), directory)
                .DownloadAndVerifyAsync(manifest, cancellationToken: cancellation.Token);

            await contentStream.FirstRead.Task.WaitAsync(TimeSpan.FromSeconds(3));
            await WaitForPartialFileAsync(directory);
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => download);
            Assert.Empty(Directory.EnumerateFiles(directory));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Downloader_RejectsDeclaredOversizedContent()
    {
        var manifest = Manifest(new string('A', 64));
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new DeclaredLengthContent(250L * 1024 * 1024 + 1)
        });
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvasUpdateTests", Guid.NewGuid().ToString("N"));

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new UpdatePackageDownloader(
                    httpClient: client,
                    authenticityVerifier: new AcceptingAuthenticityVerifier(),
                    downloadDirectory: directory)
                    .DownloadAndVerifyAsync(manifest));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Downloader_RejectsPublisherVerificationFailureAndCleansUp()
    {
        var bytes = Encoding.UTF8.GetBytes("installer bytes");
        var manifest = Manifest(Convert.ToHexString(SHA256.HashData(bytes)));
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        });
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvasUpdateTests", Guid.NewGuid().ToString("N"));

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new UpdatePackageDownloader(client, new RejectingAuthenticityVerifier(), directory).DownloadAndVerifyAsync(manifest));
            Assert.Empty(Directory.EnumerateFiles(directory));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Downloader_RejectsMissingBundleAndCleansUpInstaller()
    {
        var bytes = Encoding.UTF8.GetBytes("installer bytes");
        var manifest = Manifest(Convert.ToHexString(SHA256.HashData(bytes)));
        using var client = CreateClient(request => request.RequestUri!.AbsolutePath.EndsWith(".sigstore.json", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvasUpdateTests", Guid.NewGuid().ToString("N"));

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new UpdatePackageDownloader(client, new AcceptingAuthenticityVerifier(), directory).DownloadAndVerifyAsync(manifest));
            Assert.Empty(Directory.EnumerateFiles(directory));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static UpdateManifest Manifest(string hash) => new(
        UpdateManifestValidator.CurrentSchemaVersion, "0.3.0", UpdatePlatform.WindowsX64, UpdateTrack.Stable, "v0.3.0", new string('C', 40),
        new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe"), hash,
        new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"),
        new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe.sigstore.json"));

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new DelegateHandler(handler));

    private static async Task WaitForPartialFileAsync(string directory)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (Directory.EnumerateFiles(directory).Any())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail("The downloader did not create a partial file before cancellation.");
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }

    private sealed class BlockingHandler : HttpMessageHandler
    {
        public static TaskCompletionSource<bool> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Started.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class DeclaredLengthContent(long length) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.CompletedTask;

        protected override bool TryComputeLength(out long computedLength)
        {
            computedLength = length;
            return true;
        }
    }

    private sealed class PartiallyBlockingStream : Stream
    {
        private bool _hasReturnedBytes;

        public TaskCompletionSource<bool> FirstRead { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 1;

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(Span<byte> buffer) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (!_hasReturnedBytes)
            {
                _hasReturnedBytes = true;
                buffer.Span[0] = 1;
                FirstRead.TrySetResult(true);
                return 1;
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private int _index;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var index = Interlocked.Increment(ref _index) - 1;
            return Task.FromResult(responses[index]);
        }
    }

    private sealed class AcceptingAuthenticityVerifier : IInstallerAuthenticityVerifier
    {
        public Task VerifyAsync(string installerPath, string bundlePath, UpdateManifest manifest, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RejectingAuthenticityVerifier : IInstallerAuthenticityVerifier
    {
        public Task VerifyAsync(string installerPath, string bundlePath, UpdateManifest manifest, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("signature rejected"));
    }
}
