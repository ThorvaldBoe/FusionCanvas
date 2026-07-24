using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.App.StageTools;

public sealed class DesignFileViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isMissing;
    private bool _canPreview;
    private bool _canExport;
    private bool _isBusy;

    public DesignFileViewModel(DesignFileSummary summary)
    {
        AssetId = summary.AssetId;
        _name = summary.Name;
        _isMissing = summary.IsMissing;
        _canPreview = summary.CanPreview;
        _canExport = summary.CanExport;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid AssetId { get; }

    public string Name
    {
        get => _name;
        private set { _name = value; OnPropertyChanged(); }
    }

    public bool IsMissing
    {
        get => _isMissing;
        private set { _isMissing = value; OnPropertyChanged(); }
    }

    public bool CanPreview
    {
        get => _canPreview;
        private set { _canPreview = value; OnPropertyChanged(); }
    }

    public bool CanExport
    {
        get => _canExport;
        private set { _canExport = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class DesignStageToolViewModel : INotifyPropertyChanged
{
    private readonly IDesignFileService _designFileService;
    private readonly Func<CancellationToken> _cancellationTokenProvider;
    private string _emptyState = "No Design files yet. Import a PNG to begin.";
    private string _readOnlyReason = string.Empty;
    private bool _isReadOnly;
    private bool _isBusy;
    private DesignFileViewModel? _selectedFile;
    private string? _errorMessage;

    public DesignStageToolViewModel(IDesignFileService designFileService, Func<CancellationToken>? cancellationTokenProvider = null)
    {
        _designFileService = designFileService ?? throw new ArgumentNullException(nameof(designFileService));
        _cancellationTokenProvider = cancellationTokenProvider ?? (() => CancellationToken.None);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DesignFileViewModel> Files { get; } = [];

    public DesignFileViewModel? SelectedFile
    {
        get => _selectedFile;
        set { _selectedFile = value; OnPropertyChanged(); }
    }

    public string EmptyState
    {
        get => _emptyState;
        set { _emptyState = value; OnPropertyChanged(); }
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set { _isReadOnly = value; OnPropertyChanged(); }
    }

    public string ReadOnlyReason
    {
        get => _readOnlyReason;
        set { _readOnlyReason = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public async Task LoadAsync(Guid itemId, bool canEdit, CancellationToken cancellationToken = default)
    {
        IsReadOnly = !canEdit;
        ReadOnlyReason = canEdit ? string.Empty : "Design files are read-only while the item is protected or an earlier stage is being reviewed.";
        var files = await _designFileService.ListForItemAsync(itemId, cancellationToken).ConfigureAwait(true);
        Files.Clear();
        foreach (var summary in files)
        {
            Files.Add(new DesignFileViewModel(summary));
        }
        SelectedFile = Files.FirstOrDefault();
        EmptyState = Files.Count == 0 ? "No Design files yet. Import a PNG to begin." : string.Empty;
    }

    public async Task<bool> ImportAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default)
    {
        if (IsBusy || IsReadOnly)
        {
            return false;
        }

        IsBusy = true;
        try
        {
            var result = await _designFileService.ImportAsync(itemId, sourcePath, cancellationToken).ConfigureAwait(true);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error;
                return false;
            }

            ErrorMessage = null;
            await LoadAsync(itemId, !IsReadOnly, cancellationToken).ConfigureAwait(true);
            if (result.File is not null)
            {
                SelectedFile = Files.SingleOrDefault(file => file.AssetId == result.File.AssetId);
            }
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<Stream?> PreviewAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedFile is not { } file || !file.CanPreview || IsBusy)
        {
            return null;
        }

        file.IsBusy = true;
        try
        {
            return await _designFileService.OpenPreviewAsync(file.AssetId, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = $"Preview could not be loaded. {exception.Message}";
            return null;
        }
        finally
        {
            file.IsBusy = false;
        }
    }

    public async Task<bool> ExportAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        if (SelectedFile is not { } file || !file.CanExport || IsBusy)
        {
            return false;
        }

        file.IsBusy = true;
        try
        {
            await _designFileService.ExportCopyAsync(file.AssetId, destinationPath, cancellationToken).ConfigureAwait(true);
            ErrorMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = $"Export failed. {exception.Message}";
            return false;
        }
        finally
        {
            file.IsBusy = false;
        }
    }

    public async Task<bool> RemoveAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        if (SelectedFile is not { } file || IsBusy || IsReadOnly)
        {
            return false;
        }

        file.IsBusy = true;
        try
        {
            var result = await _designFileService.RemoveAsync(itemId, file.AssetId, cancellationToken).ConfigureAwait(true);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error;
                return false;
            }

            ErrorMessage = null;
            await LoadAsync(itemId, !IsReadOnly, cancellationToken).ConfigureAwait(true);
            SelectedFile = Files.FirstOrDefault();
            return true;
        }
        finally
        {
            file.IsBusy = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
