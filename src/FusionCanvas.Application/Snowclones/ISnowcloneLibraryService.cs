namespace FusionCanvas.Application.Snowclones;

public interface ISnowcloneLibraryService
{
    Task<SnowcloneLibraryResult> InitializeAsync(
        string? searchText = null,
        CancellationToken cancellationToken = default);

    Task<SnowcloneLibraryResult> LoadAsync(
        string? searchText = null,
        CancellationToken cancellationToken = default);

    Task<SnowcloneLibraryResult> CreateAsync(
        SnowcloneCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<SnowcloneLibraryResult> UpdateAsync(
        SnowcloneUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<SnowcloneLibraryResult> DeleteAsync(
        Guid id,
        string? searchText = null,
        CancellationToken cancellationToken = default);

    Task<SnowcloneLibraryResult> ImportAsync(
        Stream stream,
        string? searchText = null,
        CancellationToken cancellationToken = default);

    Task<SnowcloneLibraryResult> ImportBundledAsync(
        string? searchText = null,
        CancellationToken cancellationToken = default);

    Task<SnowcloneLibraryResult> ExportAsync(
        Stream stream,
        string? searchText = null,
        CancellationToken cancellationToken = default);
}
