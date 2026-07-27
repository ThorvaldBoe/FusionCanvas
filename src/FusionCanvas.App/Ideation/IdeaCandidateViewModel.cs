using FusionCanvas.Domain.Ideation;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FusionCanvas.App.Ideation;

public sealed class IdeaCandidateViewModel(string text, IdeationMode mode) : INotifyPropertyChanged
{
    private bool _isBusy;
    private string? _error;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Text { get; } = text;

    public IdeationMode Mode { get; } = mode;

    public bool IsBusy
    {
        get => _isBusy;
        internal set => SetField(ref _isBusy, value);
    }

    public string? Error
    {
        get => _error;
        internal set => SetField(ref _error, value);
    }

    public bool CanDecide => !IsBusy;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(IsBusy))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDecide)));
        }
    }
}
