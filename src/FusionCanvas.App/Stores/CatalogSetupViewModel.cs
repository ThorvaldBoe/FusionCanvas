using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;

/// <summary>Presentation state for normalized Blueprint, Placeholder, and Mockup Template setup.</summary>
public sealed class CatalogSetupViewModel : INotifyPropertyChanged
{
    private readonly ICatalogSetupService _catalog;
    private readonly IMockupTemplateSetupService _mockups;
    private BlueprintOffering? _selectedOffering;
    private OfferingOption? _selectedOption;
    private OfferingPlaceholder? _selectedPlaceholder;
    private MockupTemplate? _selectedTemplate;
    private OfferingOptionValue? _selectedColor;
    private Blueprint? _selectedBlueprint;
    private Guid? _requestedOfferingId;
    private string _optionName = string.Empty;
    private string _optionValue = string.Empty;
    private string _templateName = string.Empty;
    private string _offeringName = string.Empty;
    private string _providerNetworkCode = string.Empty;
    private string _error = string.Empty;
    private bool _isBusy;

    public CatalogSetupViewModel(ICatalogSetupService catalog, IMockupTemplateSetupService mockups)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _mockups = mockups ?? throw new ArgumentNullException(nameof(mockups));
        CreateOptionCommand = new AsyncRelayCommand(CreateOptionAsync, () => CanEdit && SelectedOffering is not null && !string.IsNullOrWhiteSpace(OptionName));
        CreateOfferingCommand = new AsyncRelayCommand(CreateOfferingAsync, () => CanEdit && SelectedBlueprint is not null && !string.IsNullOrWhiteSpace(OfferingName));
        SetDefaultPlaceholderCommand = new AsyncRelayCommand(SetDefaultPlaceholderAsync, () => CanEdit && SelectedOffering is not null && SelectedPlaceholder is not null);
        CreateOptionValueCommand = new AsyncRelayCommand(CreateOptionValueAsync, () => CanEdit && SelectedOffering is not null && SelectedOption is not null && !string.IsNullOrWhiteSpace(OptionValue));
        CreateTemplateCommand = new AsyncRelayCommand(CreateTemplateAsync, () => CanEdit && SelectedOffering is not null && SelectedPlaceholder is not null && !string.IsNullOrWhiteSpace(TemplateName));
        AddTemplateColorCommand = new AsyncRelayCommand(AddTemplateColorAsync, () => CanEdit && SelectedTemplate is not null && SelectedColor is not null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BlueprintOffering> Offerings { get; } = [];
    public ObservableCollection<Blueprint> Blueprints { get; } = [];
    public ObservableCollection<OfferingOption> Options { get; } = [];
    public ObservableCollection<OfferingOptionValue> OptionValues { get; } = [];
    public ObservableCollection<OfferingVariant> Variants { get; } = [];
    public ObservableCollection<OfferingPlaceholder> Placeholders { get; } = [];
    public ObservableCollection<MockupTemplate> Templates { get; } = [];
    public ObservableCollection<MockupTemplateColorVariant> TemplateColors { get; } = [];
    public ObservableCollection<OptionKind> OptionKinds { get; } = [OptionKind.Color, OptionKind.Size, OptionKind.Other];

    public BlueprintOffering? SelectedOffering
    {
        get => _selectedOffering;
        set
        {
            if (SetField(ref _selectedOffering, value))
            {
                RefreshOfferingCollections();
                OnPropertyChanged(nameof(SelectedOfferingId));
                OnPropertyChanged(nameof(HasSelectedOffering));
                OnPropertyChanged(nameof(IsOfferingContextUnavailable));
                NotifyCommands();
            }
        }
    }

    public Guid? SelectedOfferingId => SelectedOffering?.Id;
    public bool HasSelectedOffering => SelectedOffering is not null;
    public bool IsOfferingContextUnavailable => IsAvailable && _requestedOfferingId is not null && SelectedOffering is null;

    public Blueprint? SelectedBlueprint { get => _selectedBlueprint; set { if (SetField(ref _selectedBlueprint, value)) NotifyCommands(); } }

    public OfferingOption? SelectedOption
    {
        get => _selectedOption;
        set { if (SetField(ref _selectedOption, value)) { OnPropertyChanged(nameof(SelectedOptionId)); NotifyCommands(); } }
    }

    public Guid? SelectedOptionId => SelectedOption?.Id;

    public OfferingPlaceholder? SelectedPlaceholder
    {
        get => _selectedPlaceholder;
        set { if (SetField(ref _selectedPlaceholder, value)) { OnPropertyChanged(nameof(SelectedPlaceholderId)); NotifyCommands(); } }
    }

    public Guid? SelectedPlaceholderId => SelectedPlaceholder?.Id;

    public MockupTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set { if (SetField(ref _selectedTemplate, value)) { OnPropertyChanged(nameof(SelectedTemplateId)); NotifyCommands(); } }
    }

    public Guid? SelectedTemplateId => SelectedTemplate?.Id;

    public OfferingOptionValue? SelectedColor
    {
        get => _selectedColor;
        set { if (SetField(ref _selectedColor, value)) { NotifyCommands(); } }
    }

    public string OptionName { get => _optionName; set { if (SetField(ref _optionName, value)) NotifyCommands(); } }
    public string OfferingName { get => _offeringName; set { if (SetField(ref _offeringName, value)) NotifyCommands(); } }
    public string ProviderNetworkCode { get => _providerNetworkCode; set => SetField(ref _providerNetworkCode, value); }
    public OptionKind SelectedOptionKind { get; set; } = OptionKind.Color;
    public string OptionValue { get => _optionValue; set { if (SetField(ref _optionValue, value)) NotifyCommands(); } }
    public string TemplateName { get => _templateName; set { if (SetField(ref _templateName, value)) NotifyCommands(); } }
    public bool IsAvailable { get; private set; }
    public bool IsReadOnly { get; private set; }
    public bool CanEdit => IsAvailable && !IsReadOnly && !IsBusy;
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) { OnPropertyChanged(nameof(CanEdit)); NotifyCommands(); } } }
    public string ErrorMessage { get => _error; private set { if (SetField(ref _error, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ICommand CreateOptionCommand { get; }
    public ICommand CreateOfferingCommand { get; }
    public ICommand SetDefaultPlaceholderCommand { get; }
    public ICommand CreateOptionValueCommand { get; }
    public ICommand CreateTemplateCommand { get; }
    public ICommand AddTemplateColorCommand { get; }

    public async Task LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var catalog = await _catalog.LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(true);
            var mockups = await _mockups.LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(true);
            IsAvailable = true;
            IsReadOnly = catalog.IsReadOnly || mockups.IsReadOnly;
            Replace(Blueprints, catalog.Blueprints);
            Replace(Offerings, catalog.Offerings);
            Replace(Options, catalog.Options);
            Replace(OptionValues, catalog.OptionValues);
            Replace(Variants, catalog.Variants);
            Replace(Placeholders, catalog.Placeholders);
            Replace(Templates, mockups.Templates);
            Replace(TemplateColors, mockups.Colors);
            SelectedOffering = ResolveSelectedOffering();
            SelectedBlueprint = Blueprints.FirstOrDefault(value => value.Id == SelectedBlueprint?.Id) ?? Blueprints.FirstOrDefault();
            RefreshOfferingCollections();
            SelectedTemplate = Templates.FirstOrDefault(value => value.Id == SelectedTemplate?.Id) ?? Templates.FirstOrDefault();
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            ErrorMessage = exception.Message;
        }
        finally { IsBusy = false; OnPropertyChanged(nameof(IsAvailable)); OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(IsOfferingContextUnavailable)); }
    }

    public void SelectOffering(Guid? offeringId)
    {
        _requestedOfferingId = offeringId;
        SelectedOffering = offeringId is null
            ? null
            : Offerings.FirstOrDefault(value => value.Id == offeringId.Value);
        OnPropertyChanged(nameof(IsOfferingContextUnavailable));
    }

    private async Task CreateOptionAsync()
    {
        if (SelectedOffering is null) return;
        await RunMutationAsync(() => _catalog.CreateOptionAsync(new CreateOfferingOptionRequest(SelectedOffering.Id, SelectedOptionKind, OptionName))).ConfigureAwait(true);
        OptionName = string.Empty;
    }

    private async Task CreateOfferingAsync()
    {
        if (SelectedBlueprint is null) return;
        await RunMutationAsync(() => _catalog.CreateOfferingAsync(new CreateOfferingRequest(SelectedBlueprint.StoreId, SelectedBlueprint.Id, OfferingName, BlueprintOfferingKind.ProviderNetwork, ProviderNetworkCode: string.IsNullOrWhiteSpace(ProviderNetworkCode) ? "printify-choice" : ProviderNetworkCode))).ConfigureAwait(true);
        OfferingName = string.Empty;
    }

    private async Task SetDefaultPlaceholderAsync()
    {
        if (SelectedOffering is null || SelectedPlaceholder is null) return;
        await RunMutationAsync(() => _catalog.UpdateAsync(new UpdateCatalogRecordRequest(SelectedOffering.StoreId, CatalogRecordKind.Offering, SelectedOffering.Id, DefaultPlaceholderId: SelectedPlaceholder.Id))).ConfigureAwait(true);
    }

    private async Task CreateOptionValueAsync()
    {
        if (SelectedOffering is null || SelectedOption is null) return;
        await RunMutationAsync(() => _catalog.CreateOptionValueAsync(new CreateOptionValueRequest(SelectedOffering.Id, SelectedOption.Id, OptionValue))).ConfigureAwait(true);
        OptionValue = string.Empty;
    }

    private async Task CreateTemplateAsync()
    {
        if (SelectedOffering is null || SelectedPlaceholder is null) return;
        await RunMockupMutationAsync(() => _mockups.CreateTemplateAsync(new CreateMockupTemplateRequest(SelectedOffering.StoreId, SelectedOffering.Id, TemplateName, SelectedPlaceholder.Id))).ConfigureAwait(true);
        TemplateName = string.Empty;
    }

    private async Task AddTemplateColorAsync()
    {
        if (SelectedOffering is null || SelectedTemplate is null || SelectedColor is null) return;
        await RunMockupMutationAsync(() => _mockups.AddColorAsync(new AddMockupTemplateColorRequest(SelectedOffering.StoreId, SelectedTemplate.Id, SelectedColor.Id))).ConfigureAwait(true);
    }

    private async Task RunMutationAsync(Func<Task<CatalogSetupResult>> mutation)
    {
        IsBusy = true; ErrorMessage = string.Empty;
        try { var result = await mutation().ConfigureAwait(true); if (!result.Succeeded) ErrorMessage = result.Error ?? "Catalog change failed."; else ApplyCatalog(result.State); }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task RunMockupMutationAsync(Func<Task<MockupTemplateSetupResult>> mutation)
    {
        IsBusy = true; ErrorMessage = string.Empty;
        try { var result = await mutation().ConfigureAwait(true); if (!result.Succeeded) ErrorMessage = result.Error ?? "Mockup Template change failed."; else ApplyMockups(result.State); }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private void ApplyCatalog(CatalogSetupState state)
    {
        Replace(Blueprints, state.Blueprints); Replace(Offerings, state.Offerings); Replace(Options, state.Options); Replace(OptionValues, state.OptionValues); Replace(Variants, state.Variants); Replace(Placeholders, state.Placeholders);
        SelectedOffering = ResolveSelectedOffering();
        RefreshOfferingCollections();
    }

    private void ApplyMockups(MockupTemplateSetupState state)
    {
        Replace(Templates, state.Templates); Replace(TemplateColors, state.Colors);
        SelectedTemplate = Templates.FirstOrDefault(value => value.Id == SelectedTemplate?.Id) ?? Templates.FirstOrDefault();
    }

    private void RefreshOfferingCollections()
    {
        OnPropertyChanged(nameof(AvailableOptions)); OnPropertyChanged(nameof(AvailableValues)); OnPropertyChanged(nameof(AvailableVariants)); OnPropertyChanged(nameof(AvailablePlaceholders)); OnPropertyChanged(nameof(AvailableTemplates)); OnPropertyChanged(nameof(AvailableColors));
    }

    public IEnumerable<OfferingOption> AvailableOptions => Options.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived);
    public IEnumerable<OfferingOptionValue> AvailableValues => OptionValues.Where(value => value.OfferingId == SelectedOffering?.Id && value.OptionId == SelectedOption?.Id && !value.IsArchived);
    public IEnumerable<OfferingVariant> AvailableVariants => Variants.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived);
    public IEnumerable<OfferingPlaceholder> AvailablePlaceholders => Placeholders.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived);
    public IEnumerable<MockupTemplate> AvailableTemplates => Templates.Where(value => value.BlueprintOfferingId == SelectedOffering?.Id && !value.IsArchived);
    public IEnumerable<OfferingOptionValue> AvailableColors => OptionValues.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived && Options.Any(option => option.Id == value.OptionId && option.OptionKind == OptionKind.Color));

    private BlueprintOffering? ResolveSelectedOffering()
    {
        if (_requestedOfferingId is Guid requestedOfferingId)
        {
            return Offerings.FirstOrDefault(value => value.Id == requestedOfferingId);
        }

        return Offerings.FirstOrDefault(value => value.Id == SelectedOffering?.Id) ?? Offerings.FirstOrDefault();
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
    private void NotifyCommands() { (CreateOfferingCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged(); (SetDefaultPlaceholderCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged(); (CreateOptionCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged(); (CreateOptionValueCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged(); (CreateTemplateCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged(); (AddTemplateColorCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged(); }
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
