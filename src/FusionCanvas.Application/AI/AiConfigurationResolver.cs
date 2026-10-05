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
            AiRequestPurpose.Title or AiRequestPurpose.ContentRisk => AiPurposeProfileSettings.InheritGeneral,
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
        IReadOnlyList<AiModelDescriptor> models,
        IReadOnlyList<AiModelEndpointDescriptor>? endpoints = null)
    {
        return ResolveProfile(settings, ProfileFor(settings, purpose), models, endpoints);
    }

    public static AiConfigurationResolution ResolveArtwork(
        AiConfigurationSettings settings,
        IReadOnlyList<AiModelDescriptor> models)
    {
        var resolution = ResolveProfile(settings, settings.Artwork, models);
        if (resolution.Availability != AiConfigurationAvailability.Ready || resolution.Model is null)
        {
            return resolution;
        }

        if (!resolution.Model.OutputModalities.Any(modality =>
                string.Equals(modality, "image", StringComparison.OrdinalIgnoreCase)))
        {
            return new(
                AiConfigurationAvailability.ModelUnavailable,
                resolution.Profile,
                resolution.Model,
                ["The selected model does not support image output."]);
        }

        return resolution;
    }

    private static AiConfigurationResolution ResolveProfile(
        AiConfigurationSettings settings,
        AiProfileSettings profile,
        IReadOnlyList<AiModelDescriptor> models,
        IReadOnlyList<AiModelEndpointDescriptor>? endpoints = null)
    {
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

        var effective = AiParameterRegistry.Effective(profile, model);
        var routing = ResolveRouting(settings, effective, model, endpoints);
        if (!routing.IsReady)
        {
            return new(
                AiConfigurationAvailability.RoutingUnavailable,
                effective,
                model,
                routing.Errors)
            {
                Routing = routing
            };
        }

        return new(AiConfigurationAvailability.Ready, effective, model, [])
        {
            Routing = routing
        };
    }

    private static AiRoutingResolution ResolveRouting(
        AiConfigurationSettings settings,
        AiProfileSettings profile,
        AiModelDescriptor model,
        IReadOnlyList<AiModelEndpointDescriptor>? endpoints)
    {
        var policy = profile.Routing ?? AiRoutingPolicy.Automatic;
        if (!Enum.IsDefined(policy.Mode))
        {
            return new(AiRoutingAvailability.Invalid, policy, null, ["The selected routing policy is invalid."]);
        }

        if (policy.Mode == AiRoutingMode.Automatic)
        {
            return AiRoutingResolution.Automatic(policy);
        }

        if (policy.Mode == AiRoutingMode.SpecificProvider && string.IsNullOrWhiteSpace(policy.ProviderId))
        {
            return new(AiRoutingAvailability.MissingProvider, policy, null, ["Select a provider for this profile."]);
        }

        if (policy.Mode == AiRoutingMode.ExactEndpoint && string.IsNullOrWhiteSpace(policy.EndpointId))
        {
            return new(AiRoutingAvailability.MissingEndpoint, policy, null, ["Select an exact endpoint for this profile."]);
        }

        if (endpoints is null)
        {
            // A saved strict route can be dispatched without a local catalog. The
            // Integration layer remains authoritative when OpenRouter evaluates it.
            return AiRoutingResolution.Automatic(policy);
        }

        var candidates = endpoints
            .Where(endpoint => string.Equals(endpoint.ModelId, model.Id, StringComparison.Ordinal))
            .Where(endpoint => !settings.RequireZeroDataRetention || endpoint.ZeroDataRetentionCompatible)
            .ToArray();
        if (policy.Mode == AiRoutingMode.SpecificProvider)
        {
            var provider = candidates.FirstOrDefault(endpoint =>
                string.Equals(endpoint.ProviderId, policy.ProviderId, StringComparison.OrdinalIgnoreCase));
            return provider is null
                ? new(AiRoutingAvailability.ProviderUnavailable, policy, null, ["The selected provider is unavailable or incompatible with the active privacy policy."])
                : new(AiRoutingAvailability.Ready, policy, provider, []);
        }

        var endpoint = candidates.FirstOrDefault(candidate =>
            string.Equals(candidate.EndpointId, policy.EndpointId, StringComparison.Ordinal));
        return endpoint is null
            ? new(AiRoutingAvailability.EndpointUnavailable, policy, null, ["The selected endpoint is unavailable or incompatible with the active privacy and parameter requirements."])
            : new(AiRoutingAvailability.Ready, policy, endpoint, []);
    }
}
