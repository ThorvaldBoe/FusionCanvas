using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionCanvas.Application.AI;

namespace FusionCanvas.App.Settings;

public sealed class AiProfileEditorViewModel : INotifyPropertyChanged
{
    private AiProfileSettings _settings;
    private IReadOnlyList<AiModelDescriptor> _models = [];

    public AiProfileEditorViewModel(AiProfileSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SettingsChanged;

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
            OnPropertyChanged(nameof(SupportsReasoning));
            NotifyCapabilities();
        }
    }

    public IReadOnlyList<string> ModelIds => Models.Select(model => model.Id).ToArray();

    public AiModelDescriptor? SelectedModel =>
        Models.FirstOrDefault(model => string.Equals(model.Id, ModelId, StringComparison.Ordinal));

    public bool SupportsReasoning => SelectedModel?.Reasoning is not null;
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

    public AiProfileSettings Snapshot => _settings;

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
        OnPropertyChanged(string.Empty);
        OnPropertyChanged(nameof(SelectedModel));
        OnPropertyChanged(nameof(SupportsReasoning));
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

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
