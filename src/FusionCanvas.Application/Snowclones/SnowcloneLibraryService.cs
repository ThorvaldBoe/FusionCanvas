using FusionCanvas.Domain.Snowclones;

namespace FusionCanvas.Application.Snowclones;

public sealed class SnowcloneLibraryService : ISnowcloneLibraryService
{
    private readonly ISnowcloneRepository _repository;
    private readonly ISnowcloneCsvCodec _csvCodec;
    private readonly IBundledSnowcloneSource _bundledSource;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public SnowcloneLibraryService(
        ISnowcloneRepository repository,
        ISnowcloneCsvCodec csvCodec,
        IBundledSnowcloneSource bundledSource,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _csvCodec = csvCodec ?? throw new ArgumentNullException(nameof(csvCodec));
        _bundledSource = bundledSource ?? throw new ArgumentNullException(nameof(bundledSource));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<SnowcloneLibraryResult> InitializeAsync(
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        var snapshotResult = await TryLoadAsync(searchText, cancellationToken).ConfigureAwait(false);
        if (!snapshotResult.Succeeded)
        {
            return snapshotResult.Result!;
        }

        var snapshot = snapshotResult.Snapshot!;
        if (snapshot.StarterLibraryInitialized)
        {
            return SnowcloneLibraryResult.Success(BuildState(snapshot, searchText));
        }

        try
        {
            await using var stream = await _bundledSource.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            return await ImportCoreAsync(
                snapshot,
                stream,
                searchText,
                markStarterInitialized: true,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return SnowcloneLibraryResult.Failure(
                $"Unable to initialize the bundled snowclone library: {ex.Message}",
                BuildState(snapshot, searchText));
        }
    }

    public async Task<SnowcloneLibraryResult> LoadAsync(
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        var snapshotResult = await TryLoadAsync(searchText, cancellationToken).ConfigureAwait(false);
        return snapshotResult.Succeeded
            ? SnowcloneLibraryResult.Success(BuildState(snapshotResult.Snapshot!, searchText))
            : snapshotResult.Result!;
    }

    public async Task<SnowcloneLibraryResult> CreateAsync(
        SnowcloneCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshotResult = await TryLoadAsync(request.SearchText, cancellationToken).ConfigureAwait(false);
        if (!snapshotResult.Succeeded)
        {
            return snapshotResult.Result!;
        }

        var snapshot = snapshotResult.Snapshot!;
        var validation = SnowcloneTemplatePolicy.Validate(request.Phrase, request.Guidance);
        if (!validation.IsValid)
        {
            return SnowcloneLibraryResult.Failure(validation.Error!, BuildState(snapshot, request.SearchText));
        }

        if (ContainsDuplicate(snapshot.Snowclones, validation.DuplicateKey))
        {
            return SnowcloneLibraryResult.Failure(
                "A snowclone with the same normalized phrase already exists.",
                BuildState(snapshot, request.SearchText));
        }

        var now = _clock();
        var snowclone = new Snowclone(_newId(), validation.Phrase, validation.Guidance, now, now);
        var updated = snapshot with { Snowclones = [.. snapshot.Snowclones, snowclone] };

        var saveFailure = await TrySaveAsync(snapshot, updated, request.SearchText, cancellationToken).ConfigureAwait(false);
        return saveFailure ?? SnowcloneLibraryResult.Success(
            BuildState(updated, request.SearchText),
            ToSummary(snowclone));
    }

    public async Task<SnowcloneLibraryResult> UpdateAsync(
        SnowcloneUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshotResult = await TryLoadAsync(request.SearchText, cancellationToken).ConfigureAwait(false);
        if (!snapshotResult.Succeeded)
        {
            return snapshotResult.Result!;
        }

        var snapshot = snapshotResult.Snapshot!;
        var existing = snapshot.Snowclones.SingleOrDefault(candidate => candidate.Id == request.Id);
        if (existing is null)
        {
            return SnowcloneLibraryResult.Failure("Snowclone was not found.", BuildState(snapshot, request.SearchText));
        }

        var validation = SnowcloneTemplatePolicy.Validate(request.Phrase, request.Guidance);
        if (!validation.IsValid)
        {
            return SnowcloneLibraryResult.Failure(validation.Error!, BuildState(snapshot, request.SearchText));
        }

        if (ContainsDuplicate(snapshot.Snowclones, validation.DuplicateKey, request.Id))
        {
            return SnowcloneLibraryResult.Failure(
                "A snowclone with the same normalized phrase already exists.",
                BuildState(snapshot, request.SearchText));
        }

        var updatedSnowclone = existing with
        {
            Phrase = validation.Phrase,
            Guidance = validation.Guidance,
            UpdatedAt = _clock()
        };
        var updated = snapshot with
        {
            Snowclones = snapshot.Snowclones
                .Select(candidate => candidate.Id == request.Id ? updatedSnowclone : candidate)
                .ToArray()
        };

        var saveFailure = await TrySaveAsync(snapshot, updated, request.SearchText, cancellationToken).ConfigureAwait(false);
        return saveFailure ?? SnowcloneLibraryResult.Success(
            BuildState(updated, request.SearchText),
            ToSummary(updatedSnowclone));
    }

    public async Task<SnowcloneLibraryResult> DeleteAsync(
        Guid id,
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        var snapshotResult = await TryLoadAsync(searchText, cancellationToken).ConfigureAwait(false);
        if (!snapshotResult.Succeeded)
        {
            return snapshotResult.Result!;
        }

        var snapshot = snapshotResult.Snapshot!;
        var existing = snapshot.Snowclones.SingleOrDefault(candidate => candidate.Id == id);
        if (existing is null)
        {
            return SnowcloneLibraryResult.Failure("Snowclone was not found.", BuildState(snapshot, searchText));
        }

        var updated = snapshot with
        {
            Snowclones = snapshot.Snowclones.Where(candidate => candidate.Id != id).ToArray()
        };

        var saveFailure = await TrySaveAsync(snapshot, updated, searchText, cancellationToken).ConfigureAwait(false);
        return saveFailure ?? SnowcloneLibraryResult.Success(BuildState(updated, searchText), ToSummary(existing));
    }

    public async Task<SnowcloneLibraryResult> ImportAsync(
        Stream stream,
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var snapshotResult = await TryLoadAsync(searchText, cancellationToken).ConfigureAwait(false);
        if (!snapshotResult.Succeeded)
        {
            return snapshotResult.Result!;
        }

        return await ImportCoreAsync(
            snapshotResult.Snapshot!,
            stream,
            searchText,
            markStarterInitialized: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<SnowcloneLibraryResult> ImportBundledAsync(
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        var snapshotResult = await TryLoadAsync(searchText, cancellationToken).ConfigureAwait(false);
        if (!snapshotResult.Succeeded)
        {
            return snapshotResult.Result!;
        }

        var snapshot = snapshotResult.Snapshot!;
        try
        {
            await using var stream = await _bundledSource.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            return await ImportCoreAsync(
                snapshot,
                stream,
                searchText,
                markStarterInitialized: false,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return SnowcloneLibraryResult.Failure(
                $"Unable to import the bundled snowclone library: {ex.Message}",
                BuildState(snapshot, searchText));
        }
    }

    public async Task<SnowcloneLibraryResult> ExportAsync(
        Stream stream,
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var snapshotResult = await TryLoadAsync(searchText, cancellationToken).ConfigureAwait(false);
        if (!snapshotResult.Succeeded)
        {
            return snapshotResult.Result!;
        }

        var snapshot = snapshotResult.Snapshot!;
        var rows = Sort(snapshot.Snowclones)
            .Select(snowclone => new SnowcloneCsvRow(snowclone.Phrase, snowclone.Guidance, 0))
            .ToArray();

        try
        {
            await _csvCodec.WriteAsync(stream, rows, cancellationToken).ConfigureAwait(false);
            return SnowcloneLibraryResult.Success(BuildState(snapshot, searchText));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return SnowcloneLibraryResult.Failure(
                $"Unable to export the snowclone library: {ex.Message}",
                BuildState(snapshot, searchText));
        }
    }

    private async Task<SnowcloneLibraryResult> ImportCoreAsync(
        SnowcloneLibrarySnapshot snapshot,
        Stream stream,
        string? searchText,
        bool markStarterInitialized,
        CancellationToken cancellationToken)
    {
        SnowcloneCsvReadResult readResult;
        try
        {
            readResult = await _csvCodec.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return SnowcloneLibraryResult.Failure(
                $"Unable to read the snowclone CSV: {ex.Message}",
                BuildState(snapshot, searchText));
        }

        if (!readResult.Succeeded)
        {
            return SnowcloneLibraryResult.Failure(readResult.Error!, BuildState(snapshot, searchText));
        }

        var validated = new List<(SnowcloneCsvRow Row, SnowcloneTemplateValidation Validation)>();
        foreach (var row in readResult.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var validation = SnowcloneTemplatePolicy.Validate(row.Phrase, row.Guidance);
            if (!validation.IsValid)
            {
                return SnowcloneLibraryResult.Failure(
                    $"Row {row.RowNumber}: {validation.Error}",
                    BuildState(snapshot, searchText));
            }

            validated.Add((row, validation));
        }

        var duplicateKeys = snapshot.Snowclones
            .Select(snowclone => SnowcloneTemplatePolicy.CreateDuplicateKey(snowclone.Phrase))
            .ToHashSet(StringComparer.Ordinal);
        var additions = new List<Snowclone>();
        var skippedCount = 0;
        var now = _clock();

        foreach (var entry in validated)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!duplicateKeys.Add(entry.Validation.DuplicateKey))
            {
                skippedCount++;
                continue;
            }

            additions.Add(new Snowclone(
                _newId(),
                entry.Validation.Phrase,
                entry.Validation.Guidance,
                now,
                now));
        }

        var updated = snapshot with
        {
            Snowclones = [.. snapshot.Snowclones, .. additions],
            StarterLibraryInitialized = markStarterInitialized || snapshot.StarterLibraryInitialized
        };

        if (additions.Count == 0 && updated.StarterLibraryInitialized == snapshot.StarterLibraryInitialized)
        {
            return SnowcloneLibraryResult.Success(
                BuildState(snapshot, searchText),
                addedCount: 0,
                skippedCount: skippedCount);
        }

        var saveFailure = await TrySaveAsync(snapshot, updated, searchText, cancellationToken).ConfigureAwait(false);
        return saveFailure ?? SnowcloneLibraryResult.Success(
            BuildState(updated, searchText),
            addedCount: additions.Count,
            skippedCount: skippedCount);
    }

    private async Task<(bool Succeeded, SnowcloneLibrarySnapshot? Snapshot, SnowcloneLibraryResult? Result)> TryLoadAsync(
        string? searchText,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            return (true, snapshot, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (
                false,
                null,
                SnowcloneLibraryResult.Failure(
                    $"Unable to load the snowclone library: {ex.Message}",
                    BuildState(SnowcloneLibrarySnapshot.Empty, searchText)));
        }
    }

    private async Task<SnowcloneLibraryResult?> TrySaveAsync(
        SnowcloneLibrarySnapshot confirmed,
        SnowcloneLibrarySnapshot updated,
        string? searchText,
        CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return SnowcloneLibraryResult.Failure(
                $"Unable to save the snowclone library: {ex.Message}",
                BuildState(confirmed, searchText));
        }
    }

    private static bool ContainsDuplicate(
        IReadOnlyList<Snowclone> snowclones,
        string duplicateKey,
        Guid? excludedId = null) =>
        snowclones.Any(candidate =>
            candidate.Id != excludedId &&
            SnowcloneTemplatePolicy.CreateDuplicateKey(candidate.Phrase) == duplicateKey);

    private static SnowcloneLibraryState BuildState(
        SnowcloneLibrarySnapshot snapshot,
        string? searchText)
    {
        var normalizedSearch = searchText?.Trim() ?? string.Empty;
        var all = Sort(snapshot.Snowclones).Select(ToSummary).ToArray();
        var visible = normalizedSearch.Length == 0
            ? all
            : all.Where(candidate =>
                    candidate.Phrase.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    candidate.Guidance.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        return new SnowcloneLibraryState(
            all,
            visible,
            snapshot.StarterLibraryInitialized,
            normalizedSearch);
    }

    private static IEnumerable<Snowclone> Sort(IEnumerable<Snowclone> snowclones) =>
        snowclones
            .OrderBy(snowclone => snowclone.Phrase, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(snowclone => snowclone.Id);

    private static SnowcloneSummary ToSummary(Snowclone snowclone) =>
        new(
            snowclone.Id,
            snowclone.Phrase,
            snowclone.Guidance,
            snowclone.CreatedAt,
            snowclone.UpdatedAt);
}
