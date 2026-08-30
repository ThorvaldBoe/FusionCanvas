using FusionCanvas.Application.Mockups;

namespace FusionCanvas.Integration.Files;

public sealed class RasterImageMetadataReader : IRasterImageMetadataReader
{
    public Task<RasterImageInfo> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("A source image path is required.", nameof(sourcePath));
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = File.OpenRead(sourcePath);
        Span<byte> header = stackalloc byte[32];
        var read = stream.Read(header);
        if (read >= 24 && IsPng(header))
            return Task.FromResult(new RasterImageInfo(ReadInt32(header[16..20]), ReadInt32(header[20..24])));

        if (read >= 2 && header[0] == 0xFF && header[1] == 0xD8)
        {
            stream.Position = 2;
            while (stream.Position < stream.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var marker = ReadByte(stream);
                if (marker != 0xFF) continue;
                do marker = ReadByte(stream); while (marker == 0xFF);
                if (marker is 0xD8 or 0xD9) continue;
                var length = ReadUInt16(stream);
                if (length < 2) throw new InvalidDataException("The JPEG segment is invalid.");
                if (marker is >= 0xC0 and <= 0xC3 or >= 0xC5 and <= 0xC7 or >= 0xC9 and <= 0xCB or >= 0xCD and <= 0xCF)
                {
                    _ = ReadByte(stream);
                    var height = ReadUInt16(stream);
                    var width = ReadUInt16(stream);
                    return Task.FromResult(new RasterImageInfo(width, height));
                }
                stream.Position += length - 2;
            }
        }

        throw new InvalidDataException("The file is not a supported decodable PNG or JPEG image.");
    }

    private static int ReadInt32(ReadOnlySpan<byte> bytes) => (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    private static bool IsPng(ReadOnlySpan<byte> header) => header[0] == 137 && header[1] == 80 && header[2] == 78 && header[3] == 71 && header[4] == 13 && header[5] == 10 && header[6] == 26 && header[7] == 10;
    private static int ReadByte(Stream stream) => stream.ReadByte() is var value && value >= 0 ? value : throw new EndOfStreamException();
    private static int ReadUInt16(Stream stream) => (ReadByte(stream) << 8) | ReadByte(stream);
}
