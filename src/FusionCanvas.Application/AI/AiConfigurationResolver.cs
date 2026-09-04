namespace FusionCanvas.Application.AI;

public static class AiConfigurationResolver
{
    public static AiProfileSettings ProfileFor(AiConfigurationSettings settings, AiRequestPurpose purpose)
    {
        if (!settings.AdvancedMode || purpose == AiRequestPurpose.General)
        {
            return settings.General;
        }

        var purposeProfile = purpose switch
        {
            AiRequestPurpose.Ideation => settings.Ideation,
            AiRequestPurpose.Concept => settings.Concept,
            AiRequestPurpose.Sll => settings.Sll,
            AiRequestPurpose.Title => AiPurposeProfileSettings.InheritGeneral,
            _ => throw new ArgumentOutOfRangeException(nameof(purpose))
        };

        return purposeProfile.UseGeneral ? settings.General : purposeProfile.CustomProfile;
    }

    public static AiPurposeProfileSettings EnableCustom(
        AiPurposeProfileSettings purpose,
        AiProfileSettings general)
    {
        var custom = purpose.HasCustomProfile ? purpose.CustomProfile : general;
        return new AiPurposeProfileSettings(UseGeneral: false, HasCustomProfile: true, custom);
    }

    public static AiConfigurationResolution Resolve(
        AiConfigurationSettings settings,
        AiRequestPurpose purpose,
        IReadOnlyList<AiModelDescriptor> models)
    {
        var profile = ProfileFor(settings, purpose);
        if (string.IsNullOrWhiteSpace(profile.ModelId))
        {
            return new(AiConfigurationAvailability.MissingModel, profile, null, ["Select a model."]);
        }

        var model = models.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, profile.ModelId, StringComparison.Ordinal));
        if (model is null)
        {
            return new(AiConfigurationAvailability.ModelUnavailable, profile, null, ["The selected model is unavailable."]);
        }

        if (settings.RequireZeroDataRetention && !model.ZeroDataRetentionCompatible)
        {
            return new(AiConfigurationAvailability.PrivacyIncompatible, profile, model, ["The selected model is not available with Zero Data Retention."]);
        }

        var errors = AiParameterRegistry.Validate(profile, model);
        if (errors.Count > 0)
        {
            return new(AiConfigurationAvailability.InvalidParameters, profile, model, errors);
        }

        return new(AiConfigurationAvailability.Ready, AiParameterRegistry.Effective(profile, model), model, []);
    }
}
