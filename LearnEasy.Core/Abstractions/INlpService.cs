using LearnEasy.Core.Models;

namespace LearnEasy.Core.Abstractions;

/// <summary>
/// Natural-language helper backed by a local Ollama model (phi4-mini by
/// default). Every method MUST degrade gracefully — if Ollama is unreachable
/// the implementation returns a sensible rule-based fallback so a lesson is
/// never blocked on the LLM.
/// </summary>
public interface INlpService
{
    /// <summary>A short, warm hint appropriate to the requested scaffolding level.</summary>
    Task<string> GenerateHintAsync(Word word, HintLevel level, CancellationToken ct = default);

    /// <summary>One upbeat sentence reacting to the child's last attempt.</summary>
    Task<string> GenerateEncouragementAsync(Word word, bool wasCorrect, int currentStreak, CancellationToken ct = default);

    /// <summary>A kid-friendly explanation of what the word means / how it's used.</summary>
    Task<string> ExplainWordAsync(Word word, CancellationToken ct = default);
}
