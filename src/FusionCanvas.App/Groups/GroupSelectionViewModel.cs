using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FusionCanvas.Application.Groups;

namespace FusionCanvas.App.Groups;

public sealed class GroupSelectionViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private GroupDestination? _selectedDestination;
    private string? _errorMessage;

    public GroupSelectionViewModel(IReadOnlyList<GroupDestination> destinations, GroupDestination? defaultDestination)
    {
        Destinations = destinations;
        _selectedDestination = defaultDestination ?? destinations.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<GroupDestination> Destinations { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanConfirm));
        }
    }

    public GroupDestination? SelectedDestination
    {
        get => _selectedDestination;
        set
        {
            if (Equals(_selectedDestination, value)) return;
            _selectedDestination = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanConfirm));
        }
    }

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Name) && SelectedDestination is not null;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
