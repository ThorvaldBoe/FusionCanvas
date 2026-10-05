using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.Commands;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Updates;

namespace FusionCanvas.App.Settings;

public sealed class UpdateViewModel : INotifyPropertyChanged, IDisposable
{
    private IUpdateService _service;
    private CancellationTokenSource? _operationCancellation;
    private UpdateManifest? _manifest;
    private VerifiedUpdatePackage? _package;
    private UpdatePresentationStatus _status = UpdatePresentationStatus.NotChecked;
    private string? _errorMessage;
    private bool _backgroundCheckStarted;
    private bool _disposed;

    public UpdateViewModel(IUpdateService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        CheckCommand = new AsyncRelayCommand(() => CheckAsync(manual: true), CanCheck);
        UpdateCommand = new AsyncRelayCommand(DownloadAsync, CanDownload);
        InstallCommand = new AsyncRelayCommand(InstallAsync, CanInstall);
        CancelCommand = new RelayCommand(_ => Cancel(), CanCancel);
    }

    internal void ReplaceService(IUpdateService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        if (_backgroundCheckStarted || _disposed)
        {
            return;
        }

        _service = service;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UpdatePresentationStatus Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public bool IsUpdateActionVisible =>
        Status is UpdatePresentationStatus.UpdateAvailable
            or UpdatePresentationStatus.Downloading;

    public bool IsChecking => Status == UpdatePresentationStatus.Checking;

    public bool IsCheckButtonVisible => !IsChecking;

    public bool IsUpdateAvailable => Status == UpdatePresentationStatus.UpdateAvailable;

    public bool IsDownloading => Status == UpdatePresentationStatus.Downloading;

    public bool IsReadyToInstall => Status == UpdatePresentationStatus.ReadyToInstall;

    public bool HasError => Status == UpdatePresentationStatus.Error;

    public bool IsUpToDate => Status == UpdatePresentationStatus.UpToDate;

    public bool IsUnsupported => Status == UpdatePresentationStatus.Unsupported;

    public string UpdateActionText => Status switch
    {
        UpdatePresentationStatus.Downloading => "Downloading update…",
        UpdatePresentationStatus.ReadyToInstall => "Install update",
        _ => "Update available"
    };

    public string StatusMessage => Status switch
    {
        UpdatePresentationStatus.NotChecked => "Updates have not been checked yet.",
        UpdatePresentationStatus.Checking => "Checking for updates…",
        UpdatePresentationStatus.UpToDate => "FusionCanvas is up to date.",
        UpdatePresentationStatus.UpdateAvailable => $"Version {_manifest?.ProductVersion} is available.",
        UpdatePresentationStatus.Downloading => $"Downloading version {_manifest?.ProductVersion}…",
        UpdatePresentationStatus.ReadyToInstall => $"Version {_manifest?.ProductVersion} is ready. FusionCanvas will close and restart to install it.",
        UpdatePresentationStatus.Installing => "Preparing the update…",
        UpdatePresentationStatus.Unsupported => "No compatible update is available for this installation.",
        UpdatePresentationStatus.Error => ErrorMessage ?? "The update check failed. Try again.",
        _ => string.Empty
    };

    public string? AvailableVersion => _manifest?.ProductVersion;

    public ICommand CheckCommand { get; }

    public ICommand UpdateCommand { get; }

    public ICommand InstallCommand { get; }

    public ICommand CancelCommand { get; }

    public void StartBackgroundCheck()
    {
        if (_backgroundCheckStarted || _disposed)
        {
            return;
        }

        _backgroundCheckStarted = true;
        _ = CheckAsync(manual: false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
    }

    private bool CanCheck() =>
        !_disposed
        && Status is not UpdatePresentationStatus.Checking
        and not UpdatePresentationStatus.Downloading
        and not UpdatePresentationStatus.Installing;

    private bool CanDownload() => !_disposed && Status == UpdatePresentationStatus.UpdateAvailable && _manifest is not null;

    private bool CanInstall() => !_disposed && Status == UpdatePresentationStatus.ReadyToInstall && _package is not null;

    private bool CanCancel() => Status is UpdatePresentationStatus.Checking or UpdatePresentationStatus.Downloading;

    private async Task CheckAsync(bool manual)
    {
        if (!CanCheck())
        {
            return;
        }

        CancelOperation();
        var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        _manifest = null;
        _package = null;
        ErrorMessage = null;
        Status = UpdatePresentationStatus.Checking;
        NotifyCommands();

        try
        {
            var result = await _service.CheckForUpdateAsync(cancellation.Token).ConfigureAwait(true);
            if (result.IsUpdateAvailable)
            {
                _manifest = result.Manifest;
                Status = UpdatePresentationStatus.UpdateAvailable;
            }
            else
            {
                Status = result.Status switch
                {
                    UpdateCheckStatus.UpToDate => UpdatePresentationStatus.UpToDate,
                    UpdateCheckStatus.Unsupported => UpdatePresentationStatus.Unsupported,
                    _ => UpdatePresentationStatus.Unsupported
                };
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (manual)
            {
                Status = UpdatePresentationStatus.NotChecked;
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            Status = UpdatePresentationStatus.Error;
        }
        finally
        {
            if (ReferenceEquals(_operationCancellation, cancellation))
            {
                _operationCancellation = null;
            }

            cancellation.Dispose();
            NotifyCommands();
            NotifyStateProperties();
        }
    }

    private async Task DownloadAsync()
    {
        if (!CanDownload() || _manifest is null)
        {
            return;
        }

        CancelOperation();
        var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        ErrorMessage = null;
        Status = UpdatePresentationStatus.Downloading;
        NotifyCommands();
        NotifyStateProperties();

        try
        {
            _package = await _service.DownloadAsync(_manifest, cancellationToken: cancellation.Token).ConfigureAwait(true);
            Status = UpdatePresentationStatus.ReadyToInstall;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Status = UpdatePresentationStatus.UpdateAvailable;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            Status = UpdatePresentationStatus.Error;
        }
        finally
        {
            if (ReferenceEquals(_operationCancellation, cancellation))
            {
                _operationCancellation = null;
            }

            cancellation.Dispose();
            NotifyCommands();
            NotifyStateProperties();
        }
    }

    private async Task InstallAsync()
    {
        if (!CanInstall() || _package is null)
        {
            return;
        }

        ErrorMessage = null;
        Status = UpdatePresentationStatus.Installing;
        NotifyCommands();
        NotifyStateProperties();

        try
        {
            await _service.ApplyAsync(_package).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            Status = UpdatePresentationStatus.ReadyToInstall;
            NotifyCommands();
            NotifyStateProperties();
        }
    }

    private void Cancel()
    {
        if (!CanCancel())
        {
            return;
        }

        _operationCancellation?.Cancel();
    }

    private void CancelOperation()
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
    }

    private void NotifyCommands()
    {
        ((AsyncRelayCommand)CheckCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)UpdateCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)InstallCommand).NotifyCanExecuteChanged();
        ((RelayCommand)CancelCommand).NotifyCanExecuteChanged();
    }

    private void NotifyStateProperties()
    {
        OnPropertyChanged(nameof(IsUpdateActionVisible));
        OnPropertyChanged(nameof(IsChecking));
        OnPropertyChanged(nameof(IsCheckButtonVisible));
        OnPropertyChanged(nameof(IsUpdateAvailable));
        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsReadyToInstall));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsUpToDate));
        OnPropertyChanged(nameof(IsUnsupported));
        OnPropertyChanged(nameof(UpdateActionText));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(AvailableVersion));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        if (propertyName == nameof(Status))
        {
            NotifyStateProperties();
        }

        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
