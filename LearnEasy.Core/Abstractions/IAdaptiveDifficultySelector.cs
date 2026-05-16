using LearnEasy.Core.Models;

namespace LearnEasy.Core.Abstractions;

/// <summary>
/// Decides the band the next word should come from. Pure function over recent
/// history so it is trivially unit-testable and tunable.
/// </summary>
public interface IAdaptiveDifficultySelector
{
    DifficultyLevel Evaluate(DifficultyLevel current, IReadOnlyList<SpellingAttempt> recentAttempts);
}
