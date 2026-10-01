using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;

namespace FusionCanvas.Application.Telemetry;

public sealed class WorkspaceTelemetryService : ITelemetryService, IDisposable
{
    private readonly ITelemetryStore _store;
    private readonly ITelemetryWorkspaceContext _workspaceContext;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _cleanupCancellation = new();
    private readonly CancellationToken _cleanupCancellationToken;
    private readonly Timer _cleanupTimer;
    private int _disposed;
    private WorkspaceTelemetrySettings _activeSettings = WorkspaceTelemetrySettings.Default;
    private Guid? _settingsWorkspaceId;

    public WorkspaceTelemetryService(
        ITelemetryStore store,
        ITelemetryWorkspaceContext workspaceContext,
        Func<DateTimeOffset>? clock = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _cleanupCancellationToken = _cleanupCancellation.Token;
        _cleanupTimer = new Timer(_ => _ = RunScheduledCleanupAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public event EventHandler<TelemetryEntry>? EntryRecorded;

    public bool IsCaptureEnabled
    {
        get
        {
            var workspaceId = _workspaceContext.ActiveWorkspaceId;
            lock (_gate) return workspaceId is not null && _settingsWorkspaceId == workspaceId && _activeSettings.DebugModeEnabled;
        }
    }

    public async Task<WorkspaceTelemetrySettings> GetSettingsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var settings = await _store.ReadSettingsAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        if (_workspaceContext.ActiveWorkspaceId == workspaceId)
        {
            lock (_gate)
            {
                _settingsWorkspaceId = workspaceId;
                _activeSettings = settings;
            }
        }
        try
        {
            await DeleteExpiredAsync(workspaceId, settings.RetentionPeriod, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Trace.TraceError("Telemetry expiration cleanup failed: {0}", exception.GetType().Name);
        }
        return settings;
    }

    public async Task SaveSettingsAsync(Guid workspaceId, WorkspaceTelemetrySettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!Enum.IsDefined(settings.RetentionPeriod)) throw new ArgumentOutOfRangeException(nameof(settings));
        await _store.SaveSettingsAsync(workspaceId, settings, cancellationToken).ConfigureAwait(false);
        if (_workspaceContext.ActiveWorkspaceId == workspaceId)
        {
            lock (_gate)
            {
                _settingsWorkspaceId = workspaceId;
                _activeSettings = settings;
            }
        }
        await DeleteExpiredAsync(workspaceId, settings.RetentionPeriod, cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordAsync(TelemetryEventRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var workspaceId = _workspaceContext.ActiveWorkspaceId;
        if (workspaceId is null || !IsCaptureEnabled) return;

        try
        {
            var safe = TelemetrySecretRedactor.Sanitize(request);
            var entry = new TelemetryEntry(
                Guid.NewGuid(), workspaceId.Value, _clock(), safe.Area, safe.Name, safe.Severity,
                safe.Outcome, safe.Message, safe.MetadataJson, safe.RequestBody, safe.ResponseBody,
                safe.RequestDetailsJson, safe.ResponseDetailsJson, safe.CorrelationId);
            await _store.AddAsync(entry, cancellationToken).ConfigureAwait(false);
            EntryRecorded?.Invoke(this, entry);
            WorkspaceTelemetrySettings settings;
            lock (_gate) settings = _activeSettings;
            await DeleteExpiredAsync(workspaceId.Value, settings.RetentionPeriod, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Diagnostics must never replace the operation result being observed.
            Trace.TraceError("Telemetry recording failed: {0}", exception.GetType().Name);
        }
    }

    public Task<IReadOnlyList<TelemetryEntry>> SearchAsync(Guid workspaceId, TelemetryQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(query.PageSize));
        if (query.Offset < 0) throw new ArgumentOutOfRangeException(nameof(query.Offset));
        if (query.FromInclusive is { } from && query.ToExclusive is { } to && from >= to) throw new ArgumentException("The start time must be before the end time.", nameof(query));
        return _store.SearchAsync(workspaceId, query, cancellationToken);
    }

    public Task<IReadOnlyList<TelemetryEntry>> ReadAllAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        _store.ReadAllAsync(workspaceId, cancellationToken);

    public Task<int> DeleteAllAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        _store.DeleteAllAsync(workspaceId, cancellationToken);

    public async Task<string> ExportJsonAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream();
        await ExportAsync(workspaceId, stream, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public async Task ExportAsync(Guid workspaceId, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var entries = await _store.ReadAllAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(
            destination,
            entries,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CleanupExpiredAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var settings = await _store.ReadSettingsAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        return await DeleteExpiredAsync(workspaceId, settings.RetentionPeriod, cancellationToken).ConfigureAwait(false);
    }

    private Task<int> DeleteExpiredAsync(Guid workspaceId, TelemetryRetentionPeriod period, CancellationToken cancellationToken) =>
        _store.DeleteExpiredAsync(workspaceId, _clock() - (period switch
        {
            TelemetryRetentionPeriod.OneHour => TimeSpan.FromHours(1),
            TelemetryRetentionPeriod.OneDay => TimeSpan.FromDays(1),
            TelemetryRetentionPeriod.OneWeek => TimeSpan.FromDays(7),
            _ => TimeSpan.FromDays(1)
        }), cancellationToken);

    internal Task RunScheduledCleanupAsync() => CleanupActiveWorkspaceAsync(_cleanupCancellationToken);

    private async Task CleanupActiveWorkspaceAsync(CancellationToken cancellationToken)
    {
        if (_workspaceContext.ActiveWorkspaceId is not { } workspaceId) return;
        try
        {
            await CleanupExpiredAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError("Telemetry expiration cleanup failed: {0}", exception.GetType().Name);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        _cleanupCancellation.Cancel();
        _cleanupTimer.Dispose();
        _cleanupCancellation.Dispose();
    }
}
