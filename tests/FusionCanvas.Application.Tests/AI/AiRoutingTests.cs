using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Tests.AI;

public sealed class AiRoutingTests
{
    [Fact]
    public void ExistingProfileDefaultsToAutomaticRouting()
    {
        var profile = AiProfileSettings.Empty;

        Assert.Equal(AiRoutingMode.Automatic, profile.Routing.Mode);
        Assert.Null(profile.Routing.ProviderId);
        Assert.Null(profile.Routing.EndpointId);
    }

    [Fact]
    public void ResolverRejectsMissingSpecificProviderWithoutReplacingSelection()
    {
        var profile = AiProfileSettings.Empty with
        {
            ModelId = "vendor/model",
            Routing = new AiRoutingPolicy(AiRoutingMode.SpecificProvider)
        };
        var settings = AiConfigurationSettings.Default with { General = profile };
        var model = Model("vendor/model");

        var result = AiConfigurationResolver.Resolve(settings, AiRequestPurpose.General, [model]);

        Assert.False(result.IsReady);
        Assert.Equal(AiConfigurationAvailability.RoutingUnavailable, result.Availability);
        Assert.Equal("vendor/model", result.Profile!.ModelId);
        Assert.Contains("provider", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolverPreservesExplicitEndpointWhenCatalogNoLongerContainsIt()
    {
        var policy = AiRoutingPolicy.ForEndpoint("provider/variant", "provider");
        var profile = AiProfileSettings.Empty with { ModelId = "vendor/model", Routing = policy };
        var settings = AiConfigurationSettings.Default with { General = profile };

        var result = AiConfigurationResolver.Resolve(
            settings,
            AiRequestPurpose.General,
            [Model("vendor/model")],
            [new AiModelEndpointDescriptor("vendor/model", "other", "Other", "other/default", null, 1024, 256, [], true, null, null, null, null)]);

        Assert.False(result.IsReady);
        Assert.Equal(AiRoutingAvailability.EndpointUnavailable, result.Routing.Availability);
        Assert.Equal(policy, result.Profile!.Routing);
    }

    [Fact]
    public void ReceiptDoesNotInventAnActualEndpoint()
    {
        var result = AiTextResult.Success(
            "answer",
            "vendor/model",
            actualModel: "vendor/model",
            provider: "Provider",
            usage: new AiTextUsage(1, 2, 3, 0.04m),
            routing: AiRoutingPolicy.ForEndpoint("provider/variant", "provider"));

        Assert.Equal(AiRoutingMode.ExactEndpoint, result.RoutingReceipt.RoutingMode);
        Assert.Equal("provider/variant", result.RoutingReceipt.RequestedEndpoint);
        Assert.Null(result.RoutingReceipt.ActualEndpoint);
        Assert.Contains("Actual provider: Provider", result.RoutingReceipt.Summary);
        Assert.Contains("Generation: Unavailable", result.RoutingReceipt.Summary);
    }

    private static AiModelDescriptor Model(string id) =>
        new(id, id, "vendor", null, ["text"], ["text"], [], 4096, 1024, null, null, true, null);
}
