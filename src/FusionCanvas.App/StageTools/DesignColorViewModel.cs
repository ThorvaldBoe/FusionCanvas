using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.StageTools;

public sealed class DesignColorViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public DesignColorViewModel(string colorValue, bool isSelected, bool isReadOnly)
    {
        ColorValue = colorValue;
        _isSelected = isSelected;
        IsReadOnly = isReadOnly;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ColorValue { get; }

    public bool IsReadOnly { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!IsReadOnly && _isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
