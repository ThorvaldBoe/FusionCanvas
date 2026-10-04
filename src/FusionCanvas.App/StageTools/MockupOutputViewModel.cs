using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.Mockups;
using FusionCanvas.App.DocumentWindow;

namespace FusionCanvas.App.StageTools;

public sealed class MockupOutputViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ListingStageToolViewModel _parent;
    private Bitmap? _thumbnail;
    private bool _isBusy;
    private bool _isRemovalConfirmationVisible;
    private string? _errorMessage;

    public MockupOutputViewModel(MockupGenerationOutput output, ListingStageToolViewModel parent)
    {
        Output = output;
        _parent = parent;
        IsMissing = output.IsMissing;
        PreviewCommand = new RelayCommand(_ => _ = _parent.PreviewAsync(this), () => CanPreview);
        SaveCopyCommand = new RelayCommand(_ => _ = _parent.SaveCopyAsync(this), () => CanSaveCopy);
        RequestRemoveCommand = new RelayCommand(_ => _parent.RequestRemove(this), () => CanRemove);
        ConfirmRemoveCommand = new RelayCommand(_ => _ = _parent.ConfirmRemoveAsync(this), () => CanConfirmRemove);
        CancelRemoveCommand = new RelayCommand(_ => _parent.CancelRemove(this));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public MockupGenerationOutput Output { get; }
    public Guid AssetId => Output.AssetId;
    public string Name => Output.Name;
    public string ColorValue => Output.ColorValue;
    public string TemplateLabel => string.IsNullOrWhiteSpace(Output.TemplateName) ? "Mockup template" : Output.TemplateName!;
    public bool IsMissing { get; private set; }
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) NotifyCommands(); } }
    public bool IsRemovalConfirmationVisible { get => _isRemovalConfirmationVisible; private set { if (SetField(ref _isRemovalConfirmationVisible, value)) NotifyCommands(); } }
    public string? ErrorMessage { get => _errorMessage; private set => SetField(ref _errorMessage, value); }
    public string RemovalPromptMessage => $"Remove \"{Name}\"? The generated mockup and managed copy will be deleted; the source Design will remain.";
    public Bitmap? Thumbnail => _thumbnail;
    public bool CanPreview => !IsMissing && !IsBusy && !IsRemovalConfirmationVisible;
    public bool CanSaveCopy => !IsMissing && !IsBusy && !IsRemovalConfirmationVisible;
    public bool CanRemove => _parent.CanRemoveOutputs && !IsBusy && !IsRemovalConfirmationVisible;
    public bool CanConfirmRemove => IsRemovalConfirmationVisible && !IsBusy;
    public ICommand PreviewCommand { get; }
    public ICommand SaveCopyCommand { get; }
    public ICommand RequestRemoveCommand { get; }
    public ICommand ConfirmRemoveCommand { get; }
    public ICommand CancelRemoveCommand { get; }

    public void SetThumbnail(Bitmap? thumbnail, bool isMissing, string? errorMessage = null)
    {
        _thumbnail?.Dispose();
        _thumbnail = thumbnail;
        IsMissing = isMissing;
        ErrorMessage = errorMessage;
        OnPropertyChanged(nameof(Thumbnail));
        OnPropertyChanged(nameof(IsMissing));
        NotifyCommands();
    }

    internal void BeginBusy() => IsBusy = true;
    internal void EndBusy() => IsBusy = false;
    internal void ShowRemovalConfirmation() { IsRemovalConfirmationVisible = true; ErrorMessage = null; }
    internal void HideRemovalConfirmation() => IsRemovalConfirmationVisible = false;

    private void NotifyCommands()
    {
        (PreviewCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (SaveCopyCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (RequestRemoveCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (ConfirmRemoveCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));

    public void Dispose()
    {
        _thumbnail?.Dispose();
        _thumbnail = null;
        OnPropertyChanged(nameof(Thumbnail));
    }
}
