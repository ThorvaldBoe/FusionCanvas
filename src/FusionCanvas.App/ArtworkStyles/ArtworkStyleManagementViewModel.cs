using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionCanvas.App;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.ArtworkStyles;
using FusionCanvas.Domain.ArtworkStyles;

namespace FusionCanvas.App.ArtworkStyles;

public sealed class ArtworkStyleManagementViewModel : INotifyPropertyChanged
{
    private readonly IArtworkStyleManagementService _service;
    private readonly Guid _workspaceId;
    private ArtworkStyleManagementState? _state;
    private ArtworkStyle? _selected;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string? _error;
    private string? _selectedReference;
    private bool _isBusy;
    private bool _applySelectionWithoutPrompt;
    private string _draftBaseline = string.Empty;
    private readonly List<ArtworkStyleImageSelection> _images = [];

    public ArtworkStyleManagementViewModel(IArtworkStyleManagementService service, Guid workspaceId)
    {
        _service = service;
        _workspaceId = workspaceId;
        CreateCommand = new RelayCommand(_ => _ = CreateDraftAsync(), () => !IsBusy);
        SaveCommand = new RelayCommand(_ => _ = SaveAsync(), () => !IsBusy);
        DeleteCommand = new RelayCommand(_ => _ = DeleteAsync(), () => !IsBusy && SelectedStyle is not null);
        _draftBaseline = GetDraftSignature();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Saved;
    public ObservableCollection<ArtworkStyle> Styles { get; } = [];
    public ObservableCollection<string> ReferenceLabels { get; } = [];
    public string? SelectedReference { get => _selectedReference; set { _selectedReference = value; OnPropertyChanged(); } }
    public Guid? SavedStyleId { get; private set; }
    public RelayCommand CreateCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public Func<Task<IReadOnlyList<string>>>? PickImagesAsync { get; set; }
    public Func<ArtworkStyle, int, Task<bool>>? ConfirmDeleteAsync { get; set; }
    public Func<Task<bool>>? ConfirmDiscardDraftAsync { get; set; }
    public bool HasUnsavedDraft => GetDraftSignature() != _draftBaseline;

    public ArtworkStyle? SelectedStyle
    {
        get => _selected;
        set
        {
            if (_selected?.Id == value?.Id) return;
            if (!_applySelectionWithoutPrompt && HasUnsavedDraft)
            {
                _ = ConfirmAndSelectAsync(value);
                return;
            }
            ApplySelectedStyle(value);
        }
    }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUnsavedDraft)); } }
    public string Description { get => _description; set { _description = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUnsavedDraft)); } }
    public string? Error { get => _error; private set { _error = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool IsBusy { get => _isBusy; private set { _isBusy = value; OnPropertyChanged(); CreateCommand.NotifyCanExecuteChanged(); SaveCommand.NotifyCanExecuteChanged(); DeleteCommand.NotifyCanExecuteChanged(); } }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _state = await _service.LoadAsync(_workspaceId, cancellationToken);
        RefreshStyles();
        SelectedStyle = null;
    }

    public async Task AddImagesAsync()
    {
        if (PickImagesAsync is null) return;
        var paths = await PickImagesAsync();
        foreach (var path in paths) _images.Add(new ArtworkStyleImageSelection(SourcePath: path));
        SetReferenceLabels();
    }

    public void RemoveImage(string? selectedLabel)
    {
        if (selectedLabel is null) return;
        var index = ReferenceLabels.IndexOf(selectedLabel);
        if (index < 0 || index >= _images.Count) return;
        _images.RemoveAt(index);
        SetReferenceLabels();
    }

    public void MoveImage(string? selectedLabel, int direction)
    {
        var index = selectedLabel is null ? -1 : ReferenceLabels.IndexOf(selectedLabel);
        var destination = index + direction;
        if (index < 0 || destination < 0 || destination >= _images.Count) return;
        (_images[index], _images[destination]) = (_images[destination], _images[index]);
        SetReferenceLabels();
        SelectedReference = ReferenceLabels[destination];
    }

    public async Task SaveAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _service.SaveAsync(_workspaceId, SelectedStyle?.Id, Title, Description, _images.ToArray());
            Error = result.Error;
            if (!result.Succeeded || result.State is null) return;
            SavedStyleId = result.SavedStyleId;
            _state = result.State;
            RefreshStyles();
            ApplySelectedStyle(Styles.FirstOrDefault(style => style.Id == SavedStyleId));
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) { Error = $"Artwork style could not be saved. {exception.Message}"; }
        finally { IsBusy = false; }
    }

    public async Task DeleteAsync()
    {
        if (IsBusy || SelectedStyle is null) return;
        var selected = SelectedStyle!;
        var affectedCount = _state?.AssignedItemCounts.GetValueOrDefault(selected.Id) ?? 0;
        if (ConfirmDeleteAsync is not null && !await ConfirmDeleteAsync(selected, affectedCount)) return;
        IsBusy = true;
        try
        {
            var result = await _service.DeleteAsync(_workspaceId, selected.Id);
            Error = result.Error;
            if (!result.Succeeded || result.State is null) return;
            _state = result.State;
            RefreshStyles();
            _applySelectionWithoutPrompt = true;
            try { SelectedStyle = null; }
            finally { _applySelectionWithoutPrompt = false; }
            Error = $"Style deleted. {result.AffectedItemCount} Item(s) now use General.";
        }
        catch (Exception exception) { Error = $"Artwork style could not be deleted. {exception.Message}"; }
        finally { IsBusy = false; }
    }

    private async Task CreateDraftAsync()
    {
        if (HasUnsavedDraft && (ConfirmDiscardDraftAsync is null || !await ConfirmDiscardDraftAsync())) return;
        _applySelectionWithoutPrompt = true;
        try { SelectedStyle = null; }
        finally { _applySelectionWithoutPrompt = false; }
        Title = string.Empty; Description = string.Empty; _images.Clear(); ReferenceLabels.Clear(); Error = null; SetDraftBaseline();
    }
    private async Task ConfirmAndSelectAsync(ArtworkStyle? style)
    {
        if (ConfirmDiscardDraftAsync is null || !await ConfirmDiscardDraftAsync()) { OnPropertyChanged(nameof(SelectedStyle)); return; }
        _applySelectionWithoutPrompt = true;
        try { SelectedStyle = style; }
        finally { _applySelectionWithoutPrompt = false; }
    }
    private void ApplySelectedStyle(ArtworkStyle? style)
    {
        _selected = style;
        _images.Clear();
        _title = style?.Title ?? string.Empty;
        _description = style?.Description ?? string.Empty;
        if (style is not null && _state?.References.TryGetValue(style.Id, out var refs) == true)
        {
            _images.AddRange(refs.Select(x => new ArtworkStyleImageSelection(ExistingWorkspaceRelativePath: x.Reference.WorkspaceRelativePath)));
            SetReferenceLabels();
        }
        else ReferenceLabels.Clear();
        SetDraftBaseline();
        OnPropertyChanged(nameof(SelectedStyle));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(HasUnsavedDraft));
        DeleteCommand.NotifyCanExecuteChanged();
    }
    private void RefreshStyles() { Styles.Clear(); if (_state is not null) foreach (var style in _state.Styles.OrderBy(value => value.Title, StringComparer.OrdinalIgnoreCase)) Styles.Add(style); }
    private void SetReferenceLabels() { ReferenceLabels.Clear(); foreach (var image in _images) ReferenceLabels.Add(image.SourcePath is { } path ? Path.GetFileName(path) ?? path : Path.GetFileName(image.ExistingWorkspaceRelativePath) ?? image.ExistingWorkspaceRelativePath ?? "Reference image"); OnPropertyChanged(nameof(HasUnsavedDraft)); }
    private string GetDraftSignature() => $"{SelectedStyle?.Id}|{Title.Trim()}|{Description.Trim()}|{string.Join('|', _images.Select(image => image.SourcePath ?? image.ExistingWorkspaceRelativePath))}";
    private void SetDraftBaseline() { _draftBaseline = GetDraftSignature(); OnPropertyChanged(nameof(HasUnsavedDraft)); }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
