namespace FusionCanvas.Application.Mockups;

public interface IMockupSourceImageContentReader
{
    Task<MockupSourceImageContent> ReadAsync(string sourcePath, CancellationToken cancellationToken = default);
}

public sealed record MockupSourceImageContent(string MediaType, byte[] Bytes);
