using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.Mockups;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.App.DocumentWindow;

namespace FusionCanvas.App.StageTools;

public sealed class ListingStageToolViewModel : INotifyPropertyChanged
{
    private readonly IMockupGenerationService? _service;
    private string _statusSummary = string.Empty;
    private string _readOnlyReason = string.Empty;
    private string? _blockedReason;
    private string? _errorMessage;
    private bool _isReadOnly;
    private bool _isBusy;
    private Guid _itemId;
    private Guid? _selectedTemplateId;
    private MockupTemplateOptionViewModel? _selectedTemplate;
    private CancellationTokenSource? _loadCancellation;
    private int _loadGeneration;
    private readonly RelayCommand _applyCommand;

    public ListingStageToolViewModel(IMockupGenerationService? service = null)
    {
        _service = service;
        _applyCommand = new RelayCommand(parameter => _ = ApplyAsync(), () => CanApply);
        ApplyCommand = _applyCommand;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<MockupTemplateOptionViewModel> Templates { get; } = [];
    public ObservableCollection<MockupTemplateDiagnosticViewModel> TemplateDiagnostics { get; } = [];
    public ObservableCollection<MockupGenerationOutput> Outputs { get; } = [];
    public ICommand ApplyCommand { get; }
    public string StatusSummary { get => _statusSummary; private set { _statusSummary = value; OnPropertyChanged(); } }
    public bool IsReadOnly { get => _isReadOnly; private set { _isReadOnly = value; OnPropertyChanged(); NotifyApplyCanExecuteChanged(); } }
    public string ReadOnlyReason { get => _readOnlyReason; private set { _readOnlyReason = value; OnPropertyChanged(); } }
    public string? BlockedReason { get => _blockedReason; private set { _blockedReason = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBlockedReason)); NotifyApplyCanExecuteChanged(); } }
    public bool HasBlockedReason => !string.IsNullOrWhiteSpace(BlockedReason);
    public bool HasTemplateDiagnostics => TemplateDiagnostics.Count > 0;
    public string? ErrorMessage { get => _errorMessage; private set { _errorMessage = value; OnPropertyChanged(); } }
    public bool IsBusy { get => _isBusy; private set { _isBusy = value; OnPropertyChanged(); NotifyApplyCanExecuteChanged(); } }
    public bool CanApply => !IsReadOnly && !IsBusy && SelectedTemplateId is not null && string.IsNullOrWhiteSpace(BlockedReason);
    public Guid? SelectedTemplateId { get => _selectedTemplateId; private set { _selectedTemplateId = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanApply)); NotifyApplyCanExecuteChanged(); } }
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
                var guidance = string.Join(" ", diagnostic.Blockers.Select(MockupTemplateReadinessMessageTranslator.Translate));
                TemplateDiagnostics.Add(new(diagnostic.TemplateName, guidance, diagnostic.Blockers));
            }
            OnPropertyChanged(nameof(HasTemplateDiagnostics));
            SelectedTemplate = state.SelectedTemplateId is Guid selectedId
                ? Templates.SingleOrDefault(value => value.Id == selectedId)
                : null;
            Outputs.Clear();
            foreach (var output in state.Outputs) Outputs.Add(output);
            BlockedReason = state.BlockedReason;
            ErrorMessage = state.Error;
        }
        finally
        {
            Interlocked.CompareExchange(ref _loadCancellation, null, loadCancellation);
            loadCancellation.Dispose();
        }
    }

    private async Task ApplyAsync()
    {
        if (_service is null || !CanApply || SelectedTemplateId is not Guid templateId) return;
        IsBusy = true; ErrorMessage = null;
        try
        {
            var result = await _service.ApplyAsync(new(_itemId, templateId)).ConfigureAwait(true);
            foreach (var output in result.Outputs) Outputs.Add(output);
            ErrorMessage = result.Diagnostics.Count == 0 ? result.Error : string.Join(" ", result.Diagnostics.Select(value => $"{value.ColorValue}: {value.Message}"));
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));

    private void NotifyApplyCanExecuteChanged() => _applyCommand.NotifyCanExecuteChanged();
}
