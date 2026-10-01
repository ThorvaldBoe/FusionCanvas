using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Application.Assets;

namespace FusionCanvas.App.Assets;

public sealed class AssetRowViewModel : INotifyPropertyChanged, IDisposable
{
    private AssetPurposeOption _selectedPurpose;
    private Bitmap? _thumbnail;
    private bool _suppressRelabel;

    public AssetRowViewModel(AssetSummary summary, IReadOnlyList<AssetPurposeOption> purposes, AssetsViewModel parent)
    {
        Id = summary.Id;
        Name = summary.Name;
        ManagedFileName = summary.ManagedFileName;
        IsMissing = summary.IsMissing;
        ContextLabel = summary.ContextLabel;
        _thumbnail = CreateThumbnail(summary);
        Purpose = summary.Kind;
        _selectedPurpose = purposes.SingleOrDefault(option => option.Kind == summary.Kind) ?? purposes[0];
        Parent = parent;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<AssetRowViewModel, AssetKind>? RelabelRequested;

    public Guid Id { get; }
    public string Name { get; }
    public string ManagedFileName { get; }
    public bool IsMissing { get; private set; }
    public string? ContextLabel { get; }
    public AssetKind Purpose { get; private set; }
    public AssetsViewModel Parent { get; }
    public Bitmap? Thumbnail => _thumbnail;
    public bool CanPreview => Thumbnail is not null;

    public AssetPurposeOption SelectedPurpose
    {
        get => _selectedPurpose;
        set
        {
            if (ReferenceEquals(_selectedPurpose, value) || _selectedPurpose == value)
            {
                return;
            }

            _selectedPurpose = value;
            OnPropertyChanged();
            if (!_suppressRelabel && value is not null && value.Kind != Purpose)
            {
                RelabelRequested?.Invoke(this, value.Kind);
            }
        }
    }

    public void ApplyRelabel(AssetKind kind, AssetPurposeOption option)
    {
        _suppressRelabel = true;
        Purpose = kind;
        _selectedPurpose = option;
        OnPropertyChanged(nameof(SelectedPurpose));
        _suppressRelabel = false;
    }

    public void RevertPurpose(AssetPurposeOption option)
    {
        _suppressRelabel = true;
        _selectedPurpose = option;
        OnPropertyChanged(nameof(SelectedPurpose));
        _suppressRelabel = false;
    }

    public void ApplyMissing(bool isMissing)
    {
        if (IsMissing == isMissing) return;
        IsMissing = isMissing;
        OnPropertyChanged(nameof(IsMissing));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        var thumbnail = Interlocked.Exchange(ref _thumbnail, null);
        if (thumbnail is null)
        {
            return;
        }

        try
        {
            thumbnail.Dispose();
        }
        finally
        {
            OnPropertyChanged(nameof(Thumbnail));
            OnPropertyChanged(nameof(CanPreview));
        }
    }

    private static Bitmap? CreateThumbnail(AssetSummary summary)
    {
        if (summary.IsMissing || summary.ManagedFilePath is null || !IsImage(summary.ManagedFilePath))
            return null;

        try
        {
            return new Bitmap(summary.ManagedFilePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsImage(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp";
}
