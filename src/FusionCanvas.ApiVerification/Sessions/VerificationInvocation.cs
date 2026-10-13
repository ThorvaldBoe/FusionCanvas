namespace FusionCanvas.ApiVerification.Sessions;

internal sealed record VerificationInvocation(
    string Command,
    string? Provider,
    string? Scenario,
    IReadOnlyList<string> Models,
    string? RunId,
    decimal? MaximumCostUsd,
    bool Force)
{
    public static VerificationInvocation Parse(IReadOnlyList<string> args)
    {
        var command = args[0].ToLowerInvariant();
        string? provider = null;
        string? scenario = null;
        string? runId = null;
        decimal? maximumCost = null;
        var force = false;
        var models = new List<string>();
        for (var index = 1; index < args.Count; index++)
        {
            var argument = args[index];
            if (argument == "--force")
            {
                force = true;
                continue;
            }

            if (index + 1 >= args.Count || !argument.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Invalid command option: {SafeDiagnostic.Redact(argument)}");
            }

            var value = args[++index];
            switch (argument)
            {
                case "--provider": provider = value; break;
                case "--scenario": scenario = value; break;
                case "--run-id": runId = value; break;
                case "--models": models.AddRange(value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)); break;
                case "--max-cost":
                    if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
                    {
                        throw new ArgumentException("--max-cost must be a positive USD amount.");
                    }
                    maximumCost = parsed;
                    break;
                default: throw new ArgumentException($"Unknown command option: {SafeDiagnostic.Redact(argument)}");
            }
        }

        return new(command, provider, scenario, models, runId, maximumCost, force);
    }
}
