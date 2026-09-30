using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FusionCanvas.App.Stores;

/// <summary>Owns the store and niche form draft values for one canonical store scope.</summary>
public class StoreConfigurationViewModel : INotifyPropertyChanged
{
    private StoreManagementScope _scope = StoreManagementScope.Empty;
    private string _newStoreName = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public StoreManagementScope Scope
    {
        get => _scope;
        internal set => SetField(ref _scope, value);
    }

    public string NewStoreName
    {
        get => _newStoreName;
        set => SetField(ref _newStoreName, value);
    }

    private string _description = string.Empty;
    public string Description { get => _description; set => SetField(ref _description, value); }
    private string _notes = string.Empty;
    public string Notes { get => _notes; set => SetField(ref _notes, value); }
    private string _targetMarket = string.Empty;
    public string TargetMarket { get => _targetMarket; set => SetField(ref _targetMarket, value); }
    private string _brandDirection = string.Empty;
    public string BrandDirection { get => _brandDirection; set => SetField(ref _brandDirection, value); }
    private string _planningContext = string.Empty;
    public string PlanningContext { get => _planningContext; set => SetField(ref _planningContext, value); }
    private string _url = string.Empty;
    public string Url { get => _url; set => SetField(ref _url, value); }
    private string _nicheName = string.Empty;
    public string NicheName { get => _nicheName; set => SetField(ref _nicheName, value); }
    private string _nicheDescription = string.Empty;
    public string NicheDescription { get => _nicheDescription; set => SetField(ref _nicheDescription, value); }
    private string _nicheAudience = string.Empty;
    public string NicheAudience { get => _nicheAudience; set => SetField(ref _nicheAudience, value); }
    private string _nicheHumorStyle = string.Empty;
    public string NicheHumorStyle { get => _nicheHumorStyle; set => SetField(ref _nicheHumorStyle, value); }
    private string _nicheVisualStyleGuidance = string.Empty;
    public string NicheVisualStyleGuidance { get => _nicheVisualStyleGuidance; set => SetField(ref _nicheVisualStyleGuidance, value); }
    private string _nicheConstraints = string.Empty;
    public string NicheConstraints { get => _nicheConstraints; set => SetField(ref _nicheConstraints, value); }
    private string _nicheRisks = string.Empty;
    public string NicheRisks { get => _nicheRisks; set => SetField(ref _nicheRisks, value); }
    private string _nicheResearchNotes = string.Empty;
    public string NicheResearchNotes { get => _nicheResearchNotes; set => SetField(ref _nicheResearchNotes, value); }
    private string _nicheNotes = string.Empty;
    public string NicheNotes { get => _nicheNotes; set => SetField(ref _nicheNotes, value); }

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        RaisePropertyChanged(propertyName);
    }
}
