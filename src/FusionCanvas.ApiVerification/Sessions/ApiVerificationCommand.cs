using FusionCanvas.ApiVerification.Inventory;
using FusionCanvas.ApiVerification.OpenRouter;
using FusionCanvas.ApiVerification.Printify;

namespace FusionCanvas.ApiVerification.Sessions;

internal static class ApiVerificationCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            PrintHelp();
            return args.Length == 0 ? 2 : 0;
        }

        var invocation = VerificationInvocation.Parse(args);
        var configuration = VerificationConfiguration.FromEnvironment();
        switch (invocation.Command)
        {
            case "list":
                PrintInventory(invocation.Provider);
                return 0;
            case "plan":
                PrintPlan(invocation, configuration);
                return 0;
            case "run":
            case "qualify":
                if (string.IsNullOrWhiteSpace(invocation.Provider))
                {
                    Console.Error.WriteLine("Run requires --provider openrouter or printify.");
                    return 2;
                }
                var selectedProvider = NormalizeProvider(invocation.Provider);
                if (selectedProvider is not ("OpenRouter" or "Printify"))
                {
                    Console.Error.WriteLine("Provider must be openrouter or printify.");
                    return 2;
                }
                if (selectedProvider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
                {
                    var selectedScenario = invocation.Command == "qualify" ? "openrouter-qualification" : invocation.Scenario;
                    if (string.IsNullOrWhiteSpace(selectedScenario))
                    {
                        Console.Error.WriteLine("OpenRouter run requires --scenario openrouter-smoke|openrouter-catalog|openrouter-text-generation|openrouter-image-generation.");
                        return 2;
                    }
                    return await new OpenRouterApiVerifier(configuration).RunAsync(
                        selectedScenario,
                        invocation.Models,
                        invocation.MaximumCostUsd,
                        invocation.Force,
                        cancellationToken).ConfigureAwait(false);
                }
                var printifyScenario = invocation.Command == "run" ? invocation.Scenario : null;
                if (string.IsNullOrWhiteSpace(printifyScenario))
                {
                    Console.Error.WriteLine("Printify run requires --scenario printify-smoke|printify-catalog|printify-shop-products|printify-product-detail|printify-artwork-import|printify-product-lifecycle.");
                    return 2;
                }
                return await new PrintifyApiVerifier(configuration).RunAsync(printifyScenario, cancellationToken).ConfigureAwait(false);
            case "recover":
                if (!string.Equals(invocation.Provider, "printify", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("Recovery currently supports only session-owned Printify products.");
                    return 2;
                }
                return await new PrintifyApiVerifier(configuration).RecoverAsync(invocation.RunId, cancellationToken).ConfigureAwait(false);
            default:
                Console.Error.WriteLine($"Unknown command: {invocation.Command}");
                PrintHelp();
                return 2;
        }
    }

    private static void PrintInventory(string? provider)
    {
        foreach (var operation in ApiOperationInventory.Operations.Where(item => provider is null || item.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"{operation.Provider,-10} {ScenarioList(operation),-55} {operation.Id,-34} {operation.Method,-6} {operation.Path,-70} {operation.CostClassification}");
        }
    }

    private static void PrintPlan(VerificationInvocation invocation, VerificationConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(invocation.Provider))
        {
            Console.Error.WriteLine("Plan requires --provider openrouter or printify.");
            return;
        }

        var provider = NormalizeProvider(invocation.Provider);
        var operations = ApiOperationInventory.Operations
            .Where(item => item.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .Where(item => invocation.Scenario is null || item.Scenario.Equals(invocation.Scenario, StringComparison.OrdinalIgnoreCase)
                || item.AdditionalScenarios?.Contains(invocation.Scenario, StringComparer.OrdinalIgnoreCase) == true);
        Console.WriteLine($"Plan: {provider} / {invocation.Scenario ?? "all inventoried operations"}");
        Console.WriteLine("This is a local inventory preview; it does not contact a provider.");
        if (provider == "OpenRouter")
        {
            Console.WriteLine($"Credential: FUSIONCANVAS_OPENROUTER_TEST_API_KEY {(string.IsNullOrWhiteSpace(configuration.OpenRouterApiKey) ? "[missing]" : "[configured]")}");
            Console.WriteLine($"Session ceiling: {configuration.OpenRouterSessionCostCeiling?.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) ?? "[missing]"} USD");
        }
        else
        {
            Console.WriteLine($"Credential: FUSIONCANVAS_PRINTIFY_DEVTEST_API_KEY {(string.IsNullOrWhiteSpace(configuration.PrintifyApiKey) ? "[missing]" : "[configured]")}");
            Console.WriteLine($"Target: {configuration.PrintifyDevTestShopName} (shop ID resolved from the API by exact title during the confirmed run)");
        }

        foreach (var operation in operations)
        {
            Console.WriteLine($"- {operation.Id}: {operation.Method} {operation.Path} [{operation.Effect}; {operation.CostClassification}]");
        }

        Console.WriteLine("A live run requires the explicit `run` or `qualify` command and an interactive confirmation.");
    }

    private static string NormalizeProvider(string provider) => provider.ToLowerInvariant() switch
    {
        "openrouter" => "OpenRouter",
        "printify" => "Printify",
        _ => provider
    };

    private static string ScenarioList(ApiOperationDefinition operation) => operation.AdditionalScenarios is { Count: > 0 } scenarios
        ? $"{operation.Scenario}, {string.Join(", ", scenarios)}"
        : operation.Scenario;

    private static void PrintHelp()
    {
        Console.WriteLine("FusionCanvas manual API verification");
        Console.WriteLine("  dotnet run --project .\\src\\FusionCanvas.ApiVerification -- list [--provider openrouter|printify]");
        Console.WriteLine("  dotnet run --project .\\src\\FusionCanvas.ApiVerification -- plan --provider openrouter|printify [--scenario ID]");
        Console.WriteLine("  dotnet run --project .\\src\\FusionCanvas.ApiVerification -- run --provider openrouter|printify --scenario ID");
        Console.WriteLine("  dotnet run --project .\\src\\FusionCanvas.ApiVerification -- qualify --provider openrouter --models all|ID[,ID...] --max-cost USD");
        Console.WriteLine("  dotnet run --project .\\src\\FusionCanvas.ApiVerification -- recover --provider printify --run-id ID");
    }
}
