using FusionCanvas.Domain.Mockups;
namespace FusionCanvas.Application.Mockups;
public interface IMockupRasterCompositor
{
    Task<Stream> ComposeAsync(Stream template, Stream design, MockupImageSpaceMapping mapping, CancellationToken cancellationToken = default);
}
