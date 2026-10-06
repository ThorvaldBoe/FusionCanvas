using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.Application.AI;

namespace FusionCanvas.App.Settings;

public sealed class AiProfileEditorViewModel : INotifyPropertyChanged
{
    private AiProfileSettings _settings;
    private IReadOnlyList<AiModelDescriptor> _models = [];
    private IReadOnlyList<AiModelEndpointDescriptor> _endpoints = [];

    public AiProfileEditorViewModel(AiProfileSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SettingsChanged;

    public ICommand? LoadEndpointsCommand { get; set; }

    public IReadOnlyList<AiRoutingMode> RoutingModes { get; } =
        [AiRoutingMode.Automatic, AiRoutingMode.SpecificProvider, AiRoutingMode.ExactEndpoint];

    public IReadOnlyList<AiModelEndpointDescriptor> Endpoints
    {
        get => _endpoints;
        private set
        {
            _endpoints = value ?? [];
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProviderIds));
            OnPropertyChanged(nameof(EndpointIds));
            OnPropertyChanged(nameof(SelectedEndpoint));
            OnPropertyChanged(nameof(EndpointMetadataAvailable));
            OnPropertyChanged(nameof(EndpointMetadataStale));
            OnPropertyChanged(nameof(EndpointMetadataStatus));
            OnPropertyChanged(nameof(EndpointSummary));
        }
    }

    public IReadOnlyList<string> ProviderIds => Endpoints
        .Select(endpoint => endpoint.ProviderId)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public IReadOnlyList<string> EndpointIds => Endpoints
        .Where(endpoint => RoutingMode != AiRoutingMode.SpecificProvider ||
            string.Equals(endpoint.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase))
        .Select(endpoint => endpoint.EndpointId)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public bool EndpointMetadataAvailable => Endpoints.Count > 0;
    public bool EndpointMetadataStale => Endpoints.Any(endpoint => endpoint.IsStale);
    public string EndpointMetadataStatus => EndpointMetadataStale
        ? "Endpoint information is from a previous refresh. Refresh before relying on current capabilities."
        : EndpointMetadataAvailable
            ? "Endpoint information is current for the last successful refresh."
            : "Endpoint information is unavailable; refresh to choose one.";

    public bool IsProviderRouting => RoutingMode == AiRoutingMode.SpecificProvider;
    public bool IsExactEndpointRouting => RoutingMode == AiRoutingMode.ExactEndpoint;

    public string RoutingExplanation => RoutingMode switch
    {
        AiRoutingMode.Automatic => "OpenRouter may choose different providers and use fallback endpoints.",
        AiRoutingMode.SpecificProvider => "The selected supplier is fixed, but its endpoint variant may still differ.",
        AiRoutingMode.ExactEndpoint => "Only this endpoint is requested; failure is shown instead of silent fallback.",
        _ => "Choose how OpenRouter should route this profile."
    };

    public string EndpointSummary => SelectedEndpoint is { } endpoint
        ? $"{endpoint.ProviderName} · {endpoint.EndpointId} · {endpoint.ContextLength?.ToString() ?? "?"} context"
        : EndpointMetadataAvailable
            ? "Choose a provider or endpoint."
            : "Endpoint information is unavailable; refresh to choose one.";

    public IReadOnlyList<AiReasoningMode> ReasoningModes
    {
        get
        {
            var capabilities = SelectedModel?.Reasoning;
            if (capabilities is null)
            {
                return [AiReasoningMode.ProviderDefault];
            }

            var modes = new List<AiReasoningMode> { AiReasoningMode.ProviderDefault };
            if (!capabilities.Mandatory) modes.Add(AiReasoningMode.Disabled);
            if (capabilities.SupportedEfforts.Count > 0) modes.Add(AiReasoningMode.Effort);
            if (capabilities.SupportsTokenBudget) modes.Add(AiReasoningMode.TokenBudget);
            return modes;
        }
    }

    public IReadOnlyList<AiModelDescriptor> Models
    {
        get => _models;
        set
        {
            _models = value ?? [];
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModelIds));
            OnPropertyChanged(nameof(SelectedModel));
            OnPropertyChanged(nameof(HasSelectedModel));
            OnPropertyChanged(nameof(SupportsReasoning));
            NotifyRouting();
            NotifyCapabilities();
        }
    }

    public IReadOnlyList<string> ModelIds => Models.Select(model => model.Id).ToArray();

    public AiModelDescriptor? SelectedModel =>
        Models.FirstOrDefault(model => string.Equals(model.Id, ModelId, StringComparison.Ordinal));

    public bool HasSelectedModel => SelectedModel is not null;

    public bool SupportsReasoning => SelectedModel?.Reasoning is not null;
    public AiRoutingMode RoutingMode
    {
        get => (_settings.Routing ?? AiRoutingPolicy.Automatic).Mode;
        set
        {
            var current = _settings.Routing ?? AiRoutingPolicy.Automatic;
            if (current.Mode == value) return;
            Update(_settings with
            {
                Routing = value switch
                {
                    AiRoutingMode.Automatic => AiRoutingPolicy.Automatic,
                    AiRoutingMode.SpecificProvider => AiRoutingPolicy.ForProvider(current.ProviderId ?? ProviderIds.FirstOrDefault() ?? string.Empty),
                    AiRoutingMode.ExactEndpoint => AiRoutingPolicy.ForEndpoint(current.EndpointId ?? EndpointIds.FirstOrDefault() ?? string.Empty, current.ProviderId),
                    _ => AiRoutingPolicy.Automatic
                }
            });
            NotifyRouting();
        }
    }

    public string? ProviderId
    {
        get => (_settings.Routing ?? AiRoutingPolicy.Automatic).ProviderId;
        set
        {
            var policy = _settings.Routing ?? AiRoutingPolicy.Automatic;
            Update(_settings with { Routing = policy with { ProviderId = EmptyToNull(value) } });
            OnPropertyChanged(nameof(EndpointIds));
            OnPropertyChanged(nameof(SelectedEndpoint));
            OnPropertyChanged(nameof(EndpointSummary));
        }
    }

    public string? EndpointId
    {
        get => (_settings.Routing ?? AiRoutingPolicy.Automatic).EndpointId;
        set
        {
            var policy = _settings.Routing ?? AiRoutingPolicy.Automatic;
            Update(_settings with { Routing = policy with { EndpointId = EmptyToNull(value) } });
            OnPropertyChanged(nameof(SelectedEndpoint));
            OnPropertyChanged(nameof(EndpointSummary));
        }
    }

    public AiModelEndpointDescriptor? SelectedEndpoint =>
        Endpoints.FirstOrDefault(endpoint => string.Equals(endpoint.EndpointId, EndpointId, StringComparison.Ordinal));

    public AiProfileSettings Snapshot => _settings with { Routing = _settings.Routing ?? AiRoutingPolicy.Automatic };
    public IReadOnlyList<string> ReasoningEfforts => SelectedModel?.Reasoning?.SupportedEfforts ?? [];
    public bool IsReasoningEffort => ReasoningMode == AiReasoningMode.Effort;
    public bool IsReasoningTokenBudget => ReasoningMode == AiReasoningMode.TokenBudget;
    public bool SupportsMaxCompletionTokens => Supports(AiParameterRegistry.MaxCompletionTokens, "max_tokens");
    public bool SupportsTemperature => Supports(AiParameterRegistry.Temperature);
    public bool SupportsTopP => Supports(AiParameterRegistry.TopP);
    public bool SupportsTopK => Supports(AiParameterRegistry.TopK);
    public bool SupportsMinP => Supports(AiParameterRegistry.MinP);
    public bool SupportsTopA => Supports(AiParameterRegistry.TopA);
    public bool SupportsFrequencyPenalty => Supports(AiParameterRegistry.FrequencyPenalty);
    public bool SupportsPresencePenalty => Supports(AiParameterRegistry.PresencePenalty);
    public bool SupportsRepetitionPenalty => Supports(AiParameterRegistry.RepetitionPenalty);
    public bool SupportsSeed => Supports(AiParameterRegistry.Seed);
    public bool SupportsStop => Supports(AiParameterRegistry.Stop);
    public bool HasAdditionalParameters =>
        SupportsTopP || SupportsTopK || SupportsMinP || SupportsTopA ||
        SupportsFrequencyPenalty || SupportsPresencePenalty ||
        SupportsRepetitionPenalty || SupportsSeed || SupportsStop;

    public string? ModelId
    {
        get => _settings.ModelId;
        set => Update(_settings with { ModelId = EmptyToNull(value) });
    }

    public int? MaxCompletionTokens
    {
        get => _settings.MaxCompletionTokens;
        set => Update(_settings with { MaxCompletionTokens = value });
    }

    public double? Temperature
    {
        get => _settings.Temperature;
        set => Update(_settings with { Temperature = value });
    }

    public double? TopP
    {
        get => _settings.TopP;
        set => Update(_settings with { TopP = value });
    }

    public int? TopK
    {
        get => _settings.TopK;
        set => Update(_settings with { TopK = value });
    }

    public double? MinP
    {
        get => _settings.MinP;
        set => Update(_settings with { MinP = value });
    }

    public double? TopA
    {
        get => _settings.TopA;
        set => Update(_settings with { TopA = value });
    }

    public double? FrequencyPenalty
    {
        get => _settings.FrequencyPenalty;
        set => Update(_settings with { FrequencyPenalty = value });
    }

    public double? PresencePenalty
    {
        get => _settings.PresencePenalty;
        set => Update(_settings with { PresencePenalty = value });
    }

    public double? RepetitionPenalty
    {
        get => _settings.RepetitionPenalty;
        set => Update(_settings with { RepetitionPenalty = value });
    }

    public int? Seed
    {
        get => _settings.Seed;
        set => Update(_settings with { Seed = value });
    }

    public string StopSequences
    {
        get => string.Join(Environment.NewLine, _settings.StopSequences);
        set => Update(_settings with
        {
            StopSequences = value.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(4)
                .ToArray()
        });
    }

    public AiReasoningMode ReasoningMode
    {
        get => _settings.Reasoning.Mode;
        set
        {
            Update(_settings with
            {
                Reasoning = new AiReasoningSettings(value, ReasoningEffort, ReasoningTokenBudget)
            });
            OnPropertyChanged(nameof(IsReasoningEffort));
            OnPropertyChanged(nameof(IsReasoningTokenBudget));
        }
    }

    public string? ReasoningEffort
    {
        get => _settings.Reasoning.Effort;
        set => Update(_settings with
        {
            Reasoning = _settings.Reasoning with { Effort = EmptyToNull(value) }
        });
    }

    public int? ReasoningTokenBudget
    {
        get => _settings.Reasoning.TokenBudget;
        set => Update(_settings with
        {
            Reasoning = _settings.Reasoning with { TokenBudget = value }
        });
    }

    public void SetEndpoints(IReadOnlyList<AiModelEndpointDescriptor> endpoints)
    {
        Endpoints = endpoints;
        NotifyRouting();
    }

    public void Replace(AiProfileSettings settings)
    {
        _settings = settings;
        NotifyAll();
    }

    private void Update(AiProfileSettings settings)
    {
        if (_settings == settings)
        {
            return;
        }

        _settings = settings;
        NotifyAll();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(ModelId));
        OnPropertyChanged(nameof(MaxCompletionTokens));
        OnPropertyChanged(nameof(Temperature));
        OnPropertyChanged(nameof(TopP));
        OnPropertyChanged(nameof(TopK));
        OnPropertyChanged(nameof(MinP));
        OnPropertyChanged(nameof(TopA));
        OnPropertyChanged(nameof(FrequencyPenalty));
        OnPropertyChanged(nameof(PresencePenalty));
        OnPropertyChanged(nameof(RepetitionPenalty));
        OnPropertyChanged(nameof(Seed));
        OnPropertyChanged(nameof(StopSequences));
        OnPropertyChanged(nameof(ReasoningMode));
        OnPropertyChanged(nameof(ReasoningEffort));
        OnPropertyChanged(nameof(ReasoningTokenBudget));
        OnPropertyChanged(nameof(SelectedModel));
        OnPropertyChanged(nameof(HasSelectedModel));
        OnPropertyChanged(nameof(SupportsReasoning));
        NotifyRouting();
        NotifyCapabilities();
    }

    private bool Supports(string parameter, string? alias = null) =>
        SelectedModel?.SupportedParameters.Contains(parameter, StringComparer.Ordinal) == true ||
        alias is not null && SelectedModel?.SupportedParameters.Contains(alias, StringComparer.Ordinal) == true;

    private void NotifyCapabilities()
    {
        OnPropertyChanged(nameof(ReasoningModes));
        OnPropertyChanged(nameof(ReasoningEfforts));
        OnPropertyChanged(nameof(IsReasoningEffort));
        OnPropertyChanged(nameof(IsReasoningTokenBudget));
        OnPropertyChanged(nameof(SupportsMaxCompletionTokens));
        OnPropertyChanged(nameof(SupportsTemperature));
        OnPropertyChanged(nameof(SupportsTopP));
        OnPropertyChanged(nameof(SupportsTopK));
        OnPropertyChanged(nameof(SupportsMinP));
        OnPropertyChanged(nameof(SupportsTopA));
        OnPropertyChanged(nameof(SupportsFrequencyPenalty));
        OnPropertyChanged(nameof(SupportsPresencePenalty));
        OnPropertyChanged(nameof(SupportsRepetitionPenalty));
        OnPropertyChanged(nameof(SupportsSeed));
        OnPropertyChanged(nameof(SupportsStop));
        OnPropertyChanged(nameof(HasAdditionalParameters));
    }

    private void NotifyRouting()
    {
        OnPropertyChanged(nameof(RoutingMode));
        OnPropertyChanged(nameof(ProviderId));
        OnPropertyChanged(nameof(EndpointId));
        OnPropertyChanged(nameof(IsProviderRouting));
        OnPropertyChanged(nameof(IsExactEndpointRouting));
        OnPropertyChanged(nameof(RoutingExplanation));
        OnPropertyChanged(nameof(ProviderIds));
        OnPropertyChanged(nameof(EndpointIds));
        OnPropertyChanged(nameof(SelectedEndpoint));
        OnPropertyChanged(nameof(EndpointSummary));
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
