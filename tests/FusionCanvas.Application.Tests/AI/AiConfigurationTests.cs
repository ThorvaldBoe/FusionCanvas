using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Tests.AI;

public class AiConfigurationTests
{
    [Fact]
    public void Defaults_ArePrivateAndUnconfigured()
    {
        var settings = AiConfigurationSettings.Default;

        Assert.True(settings.RequireZeroDataRetention);
        Assert.False(settings.AdvancedMode);
        Assert.Null(settings.General.ModelId);
        Assert.True(settings.Ideation.UseGeneral);
        Assert.True(settings.Concept.UseGeneral);
        Assert.True(settings.Sll.UseGeneral);
    }

    [Fact]
    public void ProfileFor_UsesGeneralUntilAdvancedCustomProfileIsEnabled()
    {
        var general = Profile("general/model");
        var custom = Profile("ideation/model");
        var settings = AiConfigurationSettings.Default with
        {
            General = general,
            Ideation = new AiPurposeProfileSettings(false, true, custom)
        };

        Assert.Equal(general, AiConfigurationResolver.ProfileFor(settings, AiRequestPurpose.Ideation));

        settings = settings with { AdvancedMode = true };
        Assert.Equal(custom, AiConfigurationResolver.ProfileFor(settings, AiRequestPurpose.Ideation));
        Assert.Equal(general, AiConfigurationResolver.ProfileFor(settings, AiRequestPurpose.Concept));
        Assert.Equal(general, AiConfigurationResolver.ProfileFor(settings, AiRequestPurpose.Sll));
    }

    [Fact]
    public void ProfileFor_SllUsesItsOwnCustomProfileWhenConfigured()
    {
        var general = Profile("general/model");
        var sll = Profile("sll/model");
        var settings = AiConfigurationSettings.Default with
        {
            AdvancedMode = true,
            General = general,
            Sll = new AiPurposeProfileSettings(false, true, sll)
        };

        Assert.Equal(sll, AiConfigurationResolver.ProfileFor(settings, AiRequestPurpose.Sll));
        Assert.Equal(general, AiConfigurationResolver.ProfileFor(settings, AiRequestPurpose.Concept));
    }

    [Fact]
    public void EnableCustom_CopiesGeneralOnceAndRetainsExistingCustomProfile()
    {
        var general = Profile("general/model");
        var first = AiConfigurationResolver.EnableCustom(AiPurposeProfileSettings.InheritGeneral, general);
        var changed = first with { CustomProfile = Profile("custom/model") };

        var restored = AiConfigurationResolver.EnableCustom(changed with { UseGeneral = true }, Profile("new/general"));

        Assert.Equal(general, first.CustomProfile);
        Assert.Equal("custom/model", restored.CustomProfile.ModelId);
    }

    [Fact]
    public void Resolve_RejectsMissingUnavailablePrivacyAndInvalidParameterStates()
    {
        var model = Model("model", zdr: false, AiParameterRegistry.Temperature);
        var missing = AiConfigurationResolver.Resolve(AiConfigurationSettings.Default, AiRequestPurpose.General, [model]);
        var unavailable = AiConfigurationResolver.Resolve(
            AiConfigurationSettings.Default with { General = Profile("gone") },
            AiRequestPurpose.General,
            [model]);
        var privacy = AiConfigurationResolver.Resolve(
            AiConfigurationSettings.Default with { General = Profile("model") },
            AiRequestPurpose.General,
            [model]);
        var invalid = AiConfigurationResolver.Resolve(
            AiConfigurationSettings.Default with
            {
                RequireZeroDataRetention = false,
                General = Profile("model") with { Temperature = 3 }
            },
            AiRequestPurpose.General,
            [model]);

        Assert.Equal(AiConfigurationAvailability.MissingModel, missing.Availability);
        Assert.Equal(AiConfigurationAvailability.ModelUnavailable, unavailable.Availability);
        Assert.Equal(AiConfigurationAvailability.PrivacyIncompatible, privacy.Availability);
        Assert.Equal(AiConfigurationAvailability.InvalidParameters, invalid.Availability);
    }

    [Fact]
    public void Effective_OmitsUnsupportedValuesAndUnknownCapabilities()
    {
        var profile = Profile("model") with
        {
            Temperature = 0.7,
            TopP = 0.9,
            Seed = 42,
            Reasoning = new AiReasoningSettings(AiReasoningMode.Effort, "high")
        };
        var model = Model("model", zdr: true, AiParameterRegistry.Temperature, "future_parameter");

        var effective = AiParameterRegistry.Effective(profile, model);

        Assert.Equal(0.7, effective.Temperature);
        Assert.Null(effective.TopP);
        Assert.Null(effective.Seed);
        Assert.Equal(AiReasoningMode.ProviderDefault, effective.Reasoning.Mode);
        Assert.Contains("future_parameter", model.SupportedParameters);
    }

    private static AiProfileSettings Profile(string id) => AiProfileSettings.Empty with { ModelId = id };

    private static AiModelDescriptor Model(string id, bool zdr, params string[] parameters) =>
        new(
            id,
            id,
            null,
            null,
            ["text"],
            ["text"],
            parameters,
            8192,
            1024,
            null,
            null,
            zdr,
            null);
}
