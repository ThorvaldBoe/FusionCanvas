using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using FusionCanvas.App.Mockups;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.App.DocumentWindow;

namespace FusionCanvas.App.StageTools;

public sealed class ListingStageToolViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IMockupGenerationService? _service;
    private IMockupFilePicker _filePicker;
    private string _statusSummary = string.Empty;
    private string _readOnlyReason = string.Empty;
    private string? _blockedReason;
    private string? _errorMessage;
    private string? _feedbackMessage;
    private string? _staleMessage;
    private bool _isReadOnly;
    private bool _isBusy;
    private Guid _itemId;
    private Guid? _selectedTemplateId;
    private MockupTemplateOptionViewModel? _selectedTemplate;
    private MockupOutputViewModel? _removalCandidate;
    private CancellationTokenSource? _loadCancellation;
    private int _loadGeneration;
    private Bitmap? _previewBitmap;
    private string _previewTitle = string.Empty;
    private bool _showPreviewDialog;
    private readonly RelayCommand _applyCommand;

    public ListingStageToolViewModel(IMockupGenerationService? service = null, IMockupFilePicker? filePicker = null)
    {
        _service = service;
        _filePicker = filePicker ?? new NullMockupFilePicker();
        _applyCommand = new RelayCommand(_ => _ = ApplyAsync(), () => CanApply);
        ApplyCommand = _applyCommand;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<MockupTemplateOptionViewModel> Templates { get; } = [];
    public ObservableCollection<MockupTemplateDiagnosticViewModel> TemplateDiagnostics { get; } = [];
    public ObservableCollection<MockupOutputViewModel> Outputs { get; } = [];
    public ICommand ApplyCommand { get; }
    public string StatusSummary { get => _statusSummary; private set => SetField(ref _statusSummary, value); }
    public bool IsReadOnly { get => _isReadOnly; private set { if (SetField(ref _isReadOnly, value)) { OnPropertyChanged(nameof(CanRemoveOutputs)); NotifyApplyCanExecuteChanged(); NotifyOutputCommands(); } } }
    public string ReadOnlyReason { get => _readOnlyReason; private set => SetField(ref _readOnlyReason, value); }
    public string? BlockedReason { get => _blockedReason; private set { if (SetField(ref _blockedReason, value)) { OnPropertyChanged(nameof(HasBlockedReason)); NotifyApplyCanExecuteChanged(); } } }
    public bool HasBlockedReason => !string.IsNullOrWhiteSpace(BlockedReason);
    public bool HasTemplateDiagnostics => TemplateDiagnostics.Count > 0;
    public string? ErrorMessage { get => _errorMessage; private set { if (SetField(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string? FeedbackMessage { get => _feedbackMessage; private set { if (SetField(ref _feedbackMessage, value)) OnPropertyChanged(nameof(HasFeedback)); } }
    public bool HasFeedback => !string.IsNullOrWhiteSpace(FeedbackMessage);
    public string? StaleMessage { get => _staleMessage; private set { if (SetField(ref _staleMessage, value)) OnPropertyChanged(nameof(HasStaleMessage)); } }
    public bool HasStaleMessage => !string.IsNullOrWhiteSpace(StaleMessage);
    public bool HasOutputs => Outputs.Count > 0;
    public bool IsEmpty => !HasOutputs;
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) { NotifyApplyCanExecuteChanged(); NotifyOutputCommands(); } } }
    public bool CanApply => !IsReadOnly && !IsBusy && SelectedTemplateId is not null && string.IsNullOrWhiteSpace(BlockedReason);
    public bool CanRemoveOutputs => !IsReadOnly && !IsBusy;
    public Guid? SelectedTemplateId { get => _selectedTemplateId; private set { if (SetField(ref _selectedTemplateId, value)) { OnPropertyChanged(nameof(CanApply)); NotifyApplyCanExecuteChanged(); } } }
    public MockupTemplateOptionViewModel? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (ReferenceEquals(_selectedTemplate, value)) return;
            _selectedTemplate = value;
            OnPropertyChanged();
            SelectedTemplateId = value?.Id;
        }
    }

    public IMockupFilePicker FilePicker { get => _filePicker; set => _filePicker = value ?? new NullMockupFilePicker(); }
    public Bitmap? PreviewBitmap => _previewBitmap;
    public string PreviewTitle => _previewTitle;
    public bool ShowPreviewDialog { get => _showPreviewDialog; private set => SetField(ref _showPreviewDialog, value); }

    public void Load(ItemStatus status, bool canEdit)
    {
        IsReadOnly = !canEdit;
        ReadOnlyReason = canEdit ? string.Empty : "Listing-stage content is protected while the item is Published or Rejected.";
        StatusSummary = $"This item is currently {ItemStatuses.GetDisplayName(status)}. Configure and apply a mockup template for its selected Colors.";
    }

    public async Task LoadAsync(Guid itemId, ItemStatus status, bool canEdit, CancellationToken cancellationToken = default)
    {
        var loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previousLoadCancellation = Interlocked.Exchange(ref _loadCancellation, loadCancellation);
        previousLoadCancellation?.Cancel();
        var loadGeneration = Interlocked.Increment(ref _loadGeneration);
        _itemId = itemId;
        Load(status, canEdit);
        SelectedTemplate = null;
        Templates.Clear();
        TemplateDiagnostics.Clear();
        OnPropertyChanged(nameof(HasTemplateDiagnostics));
        ClearOutputs();
        BlockedReason = null;
        ErrorMessage = null;
        FeedbackMessage = null;
        StaleMessage = null;
        try
        {
            if (_service is null)
            {
                BlockedReason = "Mockup generation is unavailable in this runtime.";
                return;
            }

            var state = await _service.LoadAsync(itemId, !canEdit, ReadOnlyReason, loadCancellation.Token).ConfigureAwait(true);
            if (loadGeneration != Volatile.Read(ref _loadGeneration)
                || !ReferenceEquals(Volatile.Read(ref _loadCancellation), loadCancellation)
                || loadCancellation.IsCancellationRequested)
            {
                return;
            }

            Templates.Clear();
            foreach (var template in state.Templates) Templates.Add(new(template.Id, template.Name));
            TemplateDiagnostics.Clear();
            foreach (var diagnostic in state.CandidateDiagnostics)
            {
                var guidance = string.Join(Environment.NewLine, diagnostic.Blockers.Select(MockupTemplateReadinessMessageTranslator.Translate));
                TemplateDiagnostics.Add(new(diagnostic.TemplateName, guidance, diagnostic.Blockers));
            }
            OnPropertyChanged(nameof(HasTemplateDiagnostics));
            SelectedTemplate = state.SelectedTemplateId is Guid selectedId
                ? Templates.SingleOrDefault(value => value.Id == selectedId)
                : null;
            foreach (var output in state.Outputs)
            {
                var row = AddOutput(output);
                await LoadThumbnailAsync(row, loadCancellation.Token).ConfigureAwait(true);
            }
            BlockedReason = state.BlockedReason;
            ErrorMessage = state.Error;
            StaleMessage = state.Notice;
        }
        catch (OperationCanceledException) when (loadCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (loadGeneration != Volatile.Read(ref _loadGeneration)
                || !ReferenceEquals(Volatile.Read(ref _loadCancellation), loadCancellation)) return;
            Templates.Clear();
            TemplateDiagnostics.Clear();
            ClearOutputs();
            SelectedTemplate = null;
            BlockedReason = null;
            OnPropertyChanged(nameof(HasTemplateDiagnostics));
            ErrorMessage = $"Listing readiness could not be loaded. {exception.Message} Try reloading Listing or checking Store settings.";
        }
        finally
        {
            Interlocked.CompareExchange(ref _loadCancellation, null, loadCancellation);
            loadCancellation.Dispose();
        }
    }

    public async Task ApplyAsync()
    {
        if (_service is null || !CanApply || SelectedTemplateId is not Guid templateId) return;
        var itemId = _itemId;
        IsBusy = true;
        ErrorMessage = null;
        FeedbackMessage = null;
        try
        {
            var result = await _service.ApplyAsync(new(itemId, templateId)).ConfigureAwait(true);
            if (itemId != _itemId)
            {
                return;
            }
            foreach (var output in result.Outputs)
            {
                if (Outputs.All(existing => existing.AssetId != output.AssetId))
                {
                    var row = AddOutput(output);
                    await LoadThumbnailAsync(row).ConfigureAwait(true);
                }
            }
            ErrorMessage = result.Diagnostics.Count == 0 ? result.Error : string.Join(" ", result.Diagnostics.Select(value => $"{value.ColorValue}: {value.Message}"));
            if (result.Outputs.Count > 0 && result.Diagnostics.Count == 0)
            {
                StaleMessage = null;
                FeedbackMessage = $"Generated {result.Outputs.Count} mockup{(result.Outputs.Count == 1 ? string.Empty : "s")}. Review the gallery below.";
            }
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    internal async Task PreviewAsync(MockupOutputViewModel output)
    {
        if (_service is null || !output.CanPreview) return;
        var itemId = _itemId;
        output.BeginBusy();
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await using var stream = await _service.OpenPreviewAsync(itemId, output.AssetId).ConfigureAwait(true);
            if (itemId != _itemId)
            {
                return;
            }
            var bitmap = new Bitmap(stream);
            _previewBitmap?.Dispose();
            _previewBitmap = bitmap;
            _previewTitle = output.Name;
            OnPropertyChanged(nameof(PreviewBitmap));
            OnPropertyChanged(nameof(PreviewTitle));
            ShowPreviewDialog = true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            output.SetThumbnail(output.Thumbnail, true, "This mockup could not be opened. Check that its managed file still exists.");
            ErrorMessage = exception.Message;
        }
        finally
        {
            output.EndBusy();
            IsBusy = false;
        }
    }

    internal async Task SaveCopyAsync(MockupOutputViewModel output)
    {
        if (_service is null || !output.CanSaveCopy) return;
        var itemId = _itemId;
        var destination = await FilePicker.PickSaveFileAsync(output.Name).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(destination)) return;
        output.BeginBusy();
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _service.ExportCopyAsync(itemId, output.AssetId, destination).ConfigureAwait(true);
            if (itemId != _itemId)
            {
                return;
            }
            if (result.Succeeded) FeedbackMessage = $"Saved a copy of {output.Name}.";
            else ErrorMessage = result.Error;
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { ErrorMessage = exception.Message; }
        finally
        {
            output.EndBusy();
            IsBusy = false;
        }
    }

    internal void RequestRemove(MockupOutputViewModel output)
    {
        if (!CanRemoveOutputs || _removalCandidate is not null) return;
        _removalCandidate = output;
        output.ShowRemovalConfirmation();
    }

    internal async Task ConfirmRemoveAsync(MockupOutputViewModel output)
    {
        if (_service is null || !ReferenceEquals(_removalCandidate, output) || !output.CanConfirmRemove) return;
        var itemId = _itemId;
        output.BeginBusy();
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _service.RemoveAsync(itemId, output.AssetId).ConfigureAwait(true);
            if (itemId != _itemId)
            {
                return;
            }
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error;
                output.HideRemovalConfirmation();
                return;
            }
            Outputs.Remove(output);
            OnPropertyChanged(nameof(HasOutputs));
            OnPropertyChanged(nameof(IsEmpty));
            output.Dispose();
            _removalCandidate = null;
            FeedbackMessage = Outputs.Count == 0
                ? "Mockup removed. Generate a new mockup when the Design is ready."
                : "Mockup removed. The source Design was not changed.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { ErrorMessage = exception.Message; }
        finally
        {
            output.EndBusy();
            IsBusy = false;
        }
    }

    internal void CancelRemove(MockupOutputViewModel output)
    {
        if (ReferenceEquals(_removalCandidate, output)) _removalCandidate = null;
        output.HideRemovalConfirmation();
    }

    public void ClosePreviewDialog()
    {
        ShowPreviewDialog = false;
        _previewBitmap?.Dispose();
        _previewBitmap = null;
        OnPropertyChanged(nameof(PreviewBitmap));
    }

    private MockupOutputViewModel AddOutput(MockupGenerationOutput output)
    {
        var row = new MockupOutputViewModel(output, this);
        Outputs.Add(row);
        OnPropertyChanged(nameof(HasOutputs));
        OnPropertyChanged(nameof(IsEmpty));
        return row;
    }

    private async Task LoadThumbnailAsync(MockupOutputViewModel output, CancellationToken cancellationToken = default)
    {
        if (_service is null) return;
        try
        {
            await using var stream = await _service.OpenPreviewAsync(_itemId, output.AssetId, cancellationToken).ConfigureAwait(true);
            output.SetThumbnail(new Bitmap(stream), false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            output.SetThumbnail(null, true, "This generated mockup is unavailable because its managed file could not be read.");
        }
    }

    private void ClearOutputs()
    {
        foreach (var output in Outputs) output.Dispose();
        Outputs.Clear();
        OnPropertyChanged(nameof(HasOutputs));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void NotifyOutputCommands()
    {
        foreach (var output in Outputs) (output.RequestRemoveCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    private void NotifyApplyCanExecuteChanged() => _applyCommand.NotifyCanExecuteChanged();

    public void Dispose()
    {
        _loadCancellation?.Cancel();
        ClearOutputs();
        ClosePreviewDialog();
    }
}
