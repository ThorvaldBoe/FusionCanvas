using FusionCanvas.Application.Mockups;

namespace FusionCanvas.Integration.Files;

public sealed class RasterImageMetadataReader : IRasterImageMetadataReader
{
    public async Task<RasterImageInfo> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("A source image path is required.", nameof(sourcePath));
        cancellationToken.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await ReadStreamAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<RasterImageInfo> ReadStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        var header = new byte[32];
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken).ConfigureAwait(false);
        if (read >= 24 && IsPng(header))
            return new RasterImageInfo(ReadInt32(header.AsSpan(16, 4)), ReadInt32(header.AsSpan(20, 4)));

        if (read >= 2 && header[0] == 0xFF && header[1] == 0xD8)
        {
            stream.Position = 2;
            var reader = new AsyncByteReader(stream);
            while (reader.Position < stream.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var marker = await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false);
                if (marker != 0xFF) continue;
                do marker = await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false); while (marker == 0xFF);
                if (marker is 0xD8 or 0xD9) continue;
                var length = await ReadUInt16Async(reader, cancellationToken).ConfigureAwait(false);
                if (length < 2) throw new InvalidDataException("The JPEG segment is invalid.");
                if (marker is >= 0xC0 and <= 0xC3 or >= 0xC5 and <= 0xC7 or >= 0xC9 and <= 0xCB or >= 0xCD and <= 0xCF)
                {
                    _ = await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false);
                    var height = await ReadUInt16Async(reader, cancellationToken).ConfigureAwait(false);
                    var width = await ReadUInt16Async(reader, cancellationToken).ConfigureAwait(false);
                    return new RasterImageInfo(width, height);
                }

                await reader.SkipAsync(length - 2, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidDataException("The file is not a supported decodable PNG or JPEG image.");
    }

    private static async ValueTask<int> ReadUInt16Async(AsyncByteReader reader, CancellationToken cancellationToken)
    {
        var high = await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false);
        var low = await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false);
        return (high << 8) | low;
    }

    private static int ReadInt32(ReadOnlySpan<byte> bytes) => (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    private static bool IsPng(ReadOnlySpan<byte> header) => header[0] == 137 && header[1] == 80 && header[2] == 78 && header[3] == 71 && header[4] == 13 && header[5] == 10 && header[6] == 26 && header[7] == 10;

    private sealed class AsyncByteReader
    {
        private const int BufferSize = 4096;
        private readonly Stream _stream;
        private readonly byte[] _buffer = new byte[BufferSize];
        private int _bufferOffset;
        private int _bufferCount;

        public AsyncByteReader(Stream stream)
        {
            _stream = stream;
            Position = stream.Position;
        }

        public long Position { get; private set; }

        public async ValueTask<int> ReadByteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_bufferOffset == _bufferCount)
            {
                _bufferOffset = 0;
                _bufferCount = await _stream.ReadAsync(_buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (_bufferCount == 0)
                    throw new EndOfStreamException();
            }

            Position++;
            return _buffer[_bufferOffset++];
        }

        public async ValueTask SkipAsync(int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = count;
            while (remaining > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var buffered = Math.Min(remaining, _bufferCount - _bufferOffset);
                if (buffered > 0)
                {
                    _bufferOffset += buffered;
                    Position += buffered;
                    remaining -= buffered;
                    continue;
                }

                _bufferOffset = 0;
                _bufferCount = await _stream.ReadAsync(
                    _buffer.AsMemory(0, Math.Min(remaining, _buffer.Length)),
                    cancellationToken).ConfigureAwait(false);
                if (_bufferCount == 0)
                    throw new EndOfStreamException();
            }
        }
    }
}
