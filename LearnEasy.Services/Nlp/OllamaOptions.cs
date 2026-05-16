namespace LearnEasy.Services.Nlp;

/// <summary>
/// Bound from the "Ollama" config section. Endpoint is supplied by Aspire
/// (the orchestrated Ollama container) or defaults to a local install.
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>The local model to use. Defaults to the requested phi4-mini.</summary>
    public string Model { get; set; } = "phi4-mini";

    /// <summary>Low temperature keeps hints accurate and on-task for kids.</summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>Hard ceiling so a slow model never stalls a lesson.</summary>
    public int TimeoutSeconds { get; set; } = 12;
}
