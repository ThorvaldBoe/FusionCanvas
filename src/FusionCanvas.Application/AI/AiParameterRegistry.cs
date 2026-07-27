namespace FusionCanvas.Application.AI;

public static class AiParameterRegistry
{
    public const string MaxCompletionTokens = "max_completion_tokens";
    public const string Temperature = "temperature";
    public const string TopP = "top_p";
    public const string TopK = "top_k";
    public const string MinP = "min_p";
    public const string TopA = "top_a";
    public const string FrequencyPenalty = "frequency_penalty";
    public const string PresencePenalty = "presence_penalty";
    public const string RepetitionPenalty = "repetition_penalty";
    public const string Seed = "seed";
    public const string Stop = "stop";
    public const string Reasoning = "reasoning";

    public static IReadOnlySet<string> Recognized { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        MaxCompletionTokens,
        "max_tokens",
        Temperature,
        TopP,
        TopK,
        MinP,
        TopA,
        FrequencyPenalty,
        PresencePenalty,
        RepetitionPenalty,
        Seed,
        Stop,
        Reasoning
    };

    public static IReadOnlyList<string> Validate(AiProfileSettings profile, AiModelDescriptor model)
    {
        var errors = new List<string>();
        var supported = model.SupportedParameters.ToHashSet(StringComparer.Ordinal);

        ValidateRange(profile.MaxCompletionTokens, 1, model.MaxCompletionTokens, MaxCompletionTokens, supported, errors, "max_tokens");
        ValidateRange(profile.Temperature, 0, 2, Temperature, supported, errors);
        ValidateRange(profile.TopP, 0, 1, TopP, supported, errors);
        ValidateRange(profile.TopK, 1, int.MaxValue, TopK, supported, errors);
        ValidateRange(profile.MinP, 0, 1, MinP, supported, errors);
        ValidateRange(profile.TopA, 0, 1, TopA, supported, errors);
        ValidateRange(profile.FrequencyPenalty, -2, 2, FrequencyPenalty, supported, errors);
        ValidateRange(profile.PresencePenalty, -2, 2, PresencePenalty, supported, errors);
        ValidateRange(profile.RepetitionPenalty, 0, 2, RepetitionPenalty, supported, errors);

        ValidateSupported(profile.Seed, Seed, supported, errors);
        if (profile.StopSequences.Length > 4 || profile.StopSequences.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("Stop sequences must contain between zero and four non-empty values.");
        }
        else if (profile.StopSequences.Length > 0)
        {
            ValidateSupported(profile.StopSequences, Stop, supported, errors);
        }

        ValidateReasoning(profile.Reasoning, model, supported, errors);
        return errors;
    }

    public static AiProfileSettings Effective(AiProfileSettings profile, AiModelDescriptor model)
    {
        var supported = model.SupportedParameters.ToHashSet(StringComparer.Ordinal);
        bool Has(string key, string? alias = null) => supported.Contains(key) || (alias is not null && supported.Contains(alias));

        return profile with
        {
            MaxCompletionTokens = Has(MaxCompletionTokens, "max_tokens") ? profile.MaxCompletionTokens : null,
            Temperature = Has(Temperature) ? profile.Temperature : null,
            TopP = Has(TopP) ? profile.TopP : null,
            TopK = Has(TopK) ? profile.TopK : null,
            MinP = Has(MinP) ? profile.MinP : null,
            TopA = Has(TopA) ? profile.TopA : null,
            FrequencyPenalty = Has(FrequencyPenalty) ? profile.FrequencyPenalty : null,
            PresencePenalty = Has(PresencePenalty) ? profile.PresencePenalty : null,
            RepetitionPenalty = Has(RepetitionPenalty) ? profile.RepetitionPenalty : null,
            Seed = Has(Seed) ? profile.Seed : null,
            StopSequences = Has(Stop) ? profile.StopSequences : [],
            Reasoning = Has(Reasoning) ? profile.Reasoning : AiReasoningSettings.ProviderDefault
        };
    }

    private static void ValidateReasoning(
        AiReasoningSettings reasoning,
        AiModelDescriptor model,
        IReadOnlySet<string> supported,
        ICollection<string> errors)
    {
        if (reasoning.Mode == AiReasoningMode.ProviderDefault)
        {
            return;
        }

        if (!supported.Contains(Reasoning) || model.Reasoning is null)
        {
            errors.Add("The selected model does not expose reasoning configuration.");
            return;
        }

        if (reasoning.Mode == AiReasoningMode.Disabled && model.Reasoning.Mandatory)
        {
            errors.Add("The selected model requires reasoning.");
        }

        if (reasoning.Mode == AiReasoningMode.Effort &&
            (string.IsNullOrWhiteSpace(reasoning.Effort) ||
             !model.Reasoning.SupportedEfforts.Contains(reasoning.Effort, StringComparer.Ordinal)))
        {
            errors.Add("The selected reasoning effort is not supported.");
        }

        if (reasoning.Mode == AiReasoningMode.TokenBudget &&
            (!model.Reasoning.SupportsTokenBudget || reasoning.TokenBudget is null or <= 0))
        {
            errors.Add("The selected model does not support this reasoning token budget.");
        }
    }

    private static void ValidateSupported<T>(
        T? value,
        string key,
        IReadOnlySet<string> supported,
        ICollection<string> errors)
    {
        if (value is not null && !supported.Contains(key))
        {
            errors.Add($"The selected model does not support {key}.");
        }
    }

    private static void ValidateRange(
        int? value,
        int min,
        int? max,
        string key,
        IReadOnlySet<string> supported,
        ICollection<string> errors,
        string? alias = null)
    {
        if (value is null)
        {
            return;
        }

        if (!supported.Contains(key) && (alias is null || !supported.Contains(alias)))
        {
            errors.Add($"The selected model does not support {key}.");
        }
        else if (value < min || (max is not null && value > max))
        {
            errors.Add($"{key} is outside the supported range.");
        }
    }

    private static void ValidateRange(
        double? value,
        double min,
        double max,
        string key,
        IReadOnlySet<string> supported,
        ICollection<string> errors)
    {
        if (value is null)
        {
            return;
        }

        if (!supported.Contains(key))
        {
            errors.Add($"The selected model does not support {key}.");
        }
        else if (value < min || value > max)
        {
            errors.Add($"{key} is outside the supported range.");
        }
    }
}
