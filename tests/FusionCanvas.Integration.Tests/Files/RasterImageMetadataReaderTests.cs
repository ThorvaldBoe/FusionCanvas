using FusionCanvas.Integration.Files;

namespace FusionCanvas.Integration.Tests.Files;

public sealed class RasterImageMetadataReaderTests
{
    [Fact]
    public async Task ReadAsync_ReadsPngDimensionsFromIhdr()
    {
        using var image = new TemporaryImageFile(".png", CreatePng(1600, 1200));

        var result = await new RasterImageMetadataReader().ReadAsync(
            image.Path,
            TestContext.Current.CancellationToken);

        Assert.Equal(1600, result.Width);
        Assert.Equal(1200, result.Height);
    }

    [Fact]
    public async Task ReadAsync_ScansJpegSegmentsUntilSofMarker()
    {
        using var image = new TemporaryImageFile(".jpg", CreateJpeg(640, 480));

        var result = await new RasterImageMetadataReader().ReadAsync(
            image.Path,
            TestContext.Current.CancellationToken);

        Assert.Equal(640, result.Width);
        Assert.Equal(480, result.Height);
    }

    [Fact]
    public async Task ReadAsync_RejectsUnsupportedImageData()
    {
        using var image = new TemporaryImageFile(".bin", [1, 2, 3, 4]);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            new RasterImageMetadataReader().ReadAsync(image.Path, TestContext.Current.CancellationToken));

        Assert.Equal("The file is not a supported decodable PNG or JPEG image.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ReadAsync_RejectsMissingSourcePath(string sourcePath)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            new RasterImageMetadataReader().ReadAsync(sourcePath, TestContext.Current.CancellationToken));

        Assert.Equal("sourcePath", exception.ParamName);
    }

    [Fact]
    public async Task ReadAsync_HonorsCancellationBeforeOpeningFile()
    {
        using var image = new TemporaryImageFile(".png", CreatePng(10, 20));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new RasterImageMetadataReader().ReadAsync(image.Path, cancellation.Token));
    }

    [Fact]
    public async Task ReadStreamAsync_HonorsCancellationDuringJpegScan()
    {
        await using var image = new CancellationAwareJpegStream();
        using var cancellation = new CancellationTokenSource();

        var read = RasterImageMetadataReader.ReadStreamAsync(image, cancellation.Token);
        await image.WaitForScanReadAsync();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
    }

    private static byte[] CreatePng(int width, int height) =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
        (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height
    ];

    private static byte[] CreateJpeg(int width, int height) =>
    [
        0xFF, 0xD8,
        0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00,
        0xFF, 0xC0, 0x00, 0x0B, 0x08,
        (byte)(height >> 8), (byte)height,
        (byte)(width >> 8), (byte)width,
        0x01, 0x01, 0x11, 0x00
    ];

    private sealed class TemporaryImageFile : IDisposable
    {
        public TemporaryImageFile(string extension, byte[] content)
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FusionCanvas.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Path = System.IO.Path.Combine(directory, $"image{extension}");
            File.WriteAllBytes(Path, content);
        }

        public string Path { get; }

        public void Dispose()
        {
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (directory is not null && Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class CancellationAwareJpegStream : MemoryStream
    {
        private readonly TaskCompletionSource _scanReadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _headerRead;

        public CancellationAwareJpegStream()
            : base([0xFF, 0xD8])
        {
        }

        public Task WaitForScanReadAsync() => _scanReadStarted.Task;

        public override long Length => base.Length + 1;

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_headerRead)
            {
                _headerRead = true;
                return await base.ReadAsync(buffer[..2], CancellationToken.None);
            }

            _scanReadStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }
}
