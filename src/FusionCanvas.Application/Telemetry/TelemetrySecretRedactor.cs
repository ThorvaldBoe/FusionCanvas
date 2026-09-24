using System.Text.Json.Nodes;

namespace FusionCanvas.Application.Telemetry;

public static class TelemetrySecretRedactor
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "proxyauthorization", "cookie", "setcookie", "api_key", "apikey", "xapikey",
        "access_token", "refresh_token", "token", "password", "secret", "client_secret", "clientsecret",
        "credential", "credentials", "private_key", "privatekey", "xauthtoken"
    };

    public static TelemetryEventRequest Sanitize(TelemetryEventRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request with
        {
            Message = RedactText(request.Message),
            MetadataJson = RedactJson(request.MetadataJson),
            RequestBody = RedactJsonOrText(request.RequestBody),
            ResponseBody = RedactJsonOrText(request.ResponseBody),
            RequestDetailsJson = RedactJson(request.RequestDetailsJson),
            ResponseDetailsJson = RedactJson(request.ResponseDetailsJson)
        };
    }

    private static string? RedactJsonOrText(string? value) => RedactJson(value) ?? (value is null ? null : RedactText(value));

    private static string? RedactJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        try
        {
            var node = JsonNode.Parse(value);
            if (node is JsonValue rootScalar && rootScalar.TryGetValue<string>(out var rootText))
                return System.Text.Json.JsonSerializer.Serialize(RedactText(rootText));
            RedactNode(node);
            return node?.ToJsonString();
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var pair in obj.ToArray())
            {
                if (IsSensitiveName(pair.Key)) obj[pair.Key] = "[REDACTED]";
                else if (pair.Value is JsonValue scalar && scalar.TryGetValue<string>(out var text)) obj[pair.Key] = RedactText(text);
                else RedactNode(pair.Value);
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is JsonValue scalar && scalar.TryGetValue<string>(out var text))
                    array[index] = RedactText(text);
                else
                    RedactNode(array[index]);
            }
        }
    }

    private static string RedactText(string value)
    {
        value = System.Text.RegularExpressions.Regex.Replace(value, "(?i)\\bBearer\\s+[A-Za-z0-9._~+/-]+=*", "Bearer [REDACTED]");
        foreach (var name in SensitiveNames)
        {
            var normalized = name.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal);
            value = System.Text.RegularExpressions.Regex.Replace(
                value,
                $"(?i)([\\\"']?{System.Text.RegularExpressions.Regex.Escape(name)}[\\\"']?\\s*[:=]\\s*[\\\"']?)[^\\s,;&\\\"']+",
                "$1[REDACTED]");
            if (normalized.Length > 0)
            {
                value = System.Text.RegularExpressions.Regex.Replace(
                    value,
                    $"(?i)([\\\"']?{System.Text.RegularExpressions.Regex.Escape(normalized)}[\\\"']?\\s*[:=]\\s*[\\\"']?)[^\\s,;&\\\"']+",
                    "$1[REDACTED]");
            }
        }
        return value;
    }

    private static bool IsSensitiveName(string value)
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal);
        return SensitiveNames.Contains(value) || SensitiveNames.Any(name =>
            string.Equals(name.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal), normalized, StringComparison.OrdinalIgnoreCase));
    }
}
