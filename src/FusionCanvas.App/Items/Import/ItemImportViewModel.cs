using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Items;
using IItemCsvCodec = FusionCanvas.Application.Items.Import.IItemCsvCodec;
using FusionCanvas.Application.Items.Import;

namespace FusionCanvas.App.Items.Import;

public sealed class ItemImportViewModel : INotifyPropertyChanged
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly ItemTopicReference _target;
    private readonly IItemCsvImportService _importService;
    private readonly IItemCsvCodec _codec;
    private IItemCsvFilePicker _filePicker;
    private string _rawSource = string.Empty;
    private string _targetLabel;
    private string? _errorMessage;
    private string? _loadError;
    private bool _isBusy;
    private bool _hasImportCompleted;
    private Task _activeOperation = Task.CompletedTask;
    private ItemCsvParseResult _parseResult = new([], []);

    public ItemImportViewModel(
        ItemTopicReference target,
        string targetLabel,
        IItemCsvImportService importService,
        IItemCsvCodec codec,
        IItemCsvFilePicker? filePicker = null)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _targetLabel = targetLabel ?? string.Empty;
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _filePicker = filePicker ?? new NullItemCsvFilePicker();

        PickFileCommand = new RelayCommand(_ => Begin(PickFileAsync()));
        ExportSampleCommand = new RelayCommand(_ => Begin(ExportSampleAsync()));
        RunPreviewCommand = new RelayCommand(_ => RunPreview());
        ImportCommand = new RelayCommand(_ => Begin(ImportAsync()), () => CanImport);
        CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CloseRequested;

    public ObservableCollection<string> PreviewRows { get; } = [];

    public ObservableCollection<string> ErrorMessages { get; } = [];

    public string TargetLabel
    {
        get => _targetLabel;
        private set => SetField(ref _targetLabel, value);
    }

    public string RawSource
    {
        get => _rawSource;
        set
        {
            if (SetField(ref _rawSource, value ?? string.Empty))
            {
                LoadError = null;
                RaiseStateProperties();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }
    }

    public string? LoadError
    {
        get => _loadError;
        private set
        {
            if (SetField(ref _loadError, value))
            {
                OnPropertyChanged(nameof(HasLoadError));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseStateProperties();
            }
        }
    }

    public bool HasImportCompleted
    {
        get => _hasImportCompleted;
        private set
        {
            if (SetField(ref _hasImportCompleted, value))
            {
                RaiseStateProperties();
            }
        }
    }

    public bool CanImport =>
        !IsBusy && !HasImportCompleted && _parseResult.Rows.Count > 0 && !_parseResult.HasErrors;

    public bool HasPreview => _parseResult.Rows.Count > 0;

    public bool HasErrors => ErrorMessages.Count > 0;

    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadError);

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ICommand PickFileCommand { get; }

    public ICommand ExportSampleCommand { get; }

    public ICommand RunPreviewCommand { get; }

    public ICommand ImportCommand { get; }

    public ICommand CloseCommand { get; }

    public IItemCsvFilePicker FilePicker
    {
        get => _filePicker;
        set => _filePicker = value ?? new NullItemCsvFilePicker();
    }

    public Task WhenIdleAsync() => _activeOperation;

    public void RunPreview()
    {
        _parseResult = _codec.Parse(RawSource);
        PreviewRows.Clear();
        foreach (var row in _parseResult.Rows)
        {
            PreviewRows.Add($"{row.LineNumber}: {row.Title}");
        }

        ErrorMessages.Clear();
        foreach (var error in _parseResult.Errors)
        {
            ErrorMessages.Add($"Line {error.LineNumber}: {error.Message}");
        }

        ErrorMessage = null;
        RaiseStateProperties();
    }

    private async Task PickFileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var stream = await _filePicker.OpenImportAsync().ConfigureAwait(false);
        if (stream is null)
        {
            return;
        }

        await using (stream)
        {
            try
            {
                using var reader = new StreamReader(stream, StrictUtf8, detectEncodingFromByteOrderMarks: true);
                RawSource = await reader.ReadToEndAsync().ConfigureAwait(false);
                RunPreview();
            }
            catch (DecoderFallbackException)
            {
                LoadError = "The selected file is not valid UTF-8 text.";
            }
        }
    }

    private async Task ExportSampleAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var stream = await _filePicker.OpenExportAsync().ConfigureAwait(false);
        if (stream is null)
        {
            return;
        }

        await using (stream)
        {
            var bytes = StrictUtf8.GetBytes(_codec.WriteSample());
            await stream.WriteAsync(bytes).ConfigureAwait(false);
        }
    }

    private async Task ImportAsync()
    {
        if (!CanImport)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _importService
                .ImportAsync(new ItemCsvImportRequest(_target, _parseResult.Rows))
                .ConfigureAwait(false);
            if (result.Succeeded)
            {
                HasImportCompleted = true;
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = result.Errors.Count > 0 ? string.Join("; ", result.Errors) : "Import failed.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Begin(Task task)
    {
        _activeOperation = ObserveAsync(task);
    }

    private async Task ObserveAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            IsBusy = false;
        }
    }

    private void RaiseStateProperties()
    {
        OnPropertyChanged(nameof(CanImport));
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(HasErrors));
        if (ImportCommand is RelayCommand relayCommand)
        {
            relayCommand.NotifyCanExecuteChanged();
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
