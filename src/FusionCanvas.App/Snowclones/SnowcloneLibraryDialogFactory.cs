using FusionCanvas.Application.Snowclones;

namespace FusionCanvas.App.Snowclones;

public sealed class SnowcloneLibraryDialogFactory(ISnowcloneLibraryService service)
{
    private readonly ISnowcloneLibraryService _service =
        service ?? throw new ArgumentNullException(nameof(service));

    public SnowcloneLibraryWindow Create() =>
        new()
        {
            DataContext = new SnowcloneLibraryViewModel(_service)
        };
}
