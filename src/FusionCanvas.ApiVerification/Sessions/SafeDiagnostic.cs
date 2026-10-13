using System.Text.RegularExpressions;

namespace FusionCanvas.ApiVerification.Sessions;

internal static partial class SafeDiagnostic
{
    [GeneratedRegex("(?i)(bearer\\s+)[^\\s,;]+")]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex("(?i)(api[_-]?key|authorization|secret|token)(\\s*[:=]\\s*|\\\"\\s*:\\s*\\\")[^\\s,;\\\"]+")]
    private static partial Regex SecretPattern();

    public static string Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var bounded = value.Length > 512 ? value[..512] : value;
        return SecretPattern().Replace(BearerTokenPattern().Replace(bounded, "$1[REDACTED]"), "$1$2[REDACTED]")
            .Replace('\r', ' ')
            .Replace('\n', ' ');
    }
}
