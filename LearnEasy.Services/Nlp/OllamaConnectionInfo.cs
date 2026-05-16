namespace LearnEasy.Services.Nlp;

/// <summary>
/// Parses the Ollama connection value. The CommunityToolkit Aspire Ollama
/// integration injects a structured connection string
/// (<c>Endpoint=http://host:port;Model=phi4-mini</c>), while a standalone run
/// uses a plain URL from appsettings. This normalizes both.
/// </summary>
public readonly record struct OllamaConnectionInfo(string Endpoint, string? Model)
{
    public static OllamaConnectionInfo Parse(string raw)
    {
        raw = raw.Trim();

        // Plain URL form (standalone / appsettings): no key=value pairs.
        if (!raw.Contains('=', StringComparison.Ordinal))
            return new OllamaConnectionInfo(raw, null);

        string? endpoint = null, model = null;
        foreach (var part in raw.Split(';',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = part.IndexOf('=', StringComparison.Ordinal);
            if (idx <= 0) continue;

            var key = part[..idx].Trim();
            var value = part[(idx + 1)..].Trim();

            if (key.Equals("Endpoint", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Url", StringComparison.OrdinalIgnoreCase))
                endpoint ??= value;
            else if (key.Equals("Model", StringComparison.OrdinalIgnoreCase))
                model = value;
        }

        return new OllamaConnectionInfo(endpoint ?? raw, model);
    }
}
