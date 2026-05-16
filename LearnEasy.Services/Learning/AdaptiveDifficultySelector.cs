using LearnEasy.Core.Abstractions;
using LearnEasy.Core.Models;

namespace LearnEasy.Services.Learning;

/// <summary>
/// Simple, explainable adaptive rule (kept pure so it is easy to unit-test and
/// tune without a database):
///   • 3+ of the last 4 correct  → move up one band
///   • 3+ of the last 4 wrong    → move down one band
///   • otherwise                 → stay
/// Hint-heavy correct answers count as "shaky" and don't push the learner up.
/// </summary>
public sealed class AdaptiveDifficultySelector : IAdaptiveDifficultySelector
{
    private const int Window = 4;
    private const int Threshold = 3;

    public DifficultyLevel Evaluate(DifficultyLevel current, IReadOnlyList<SpellingAttempt> recentAttempts)
    {
        if (recentAttempts.Count < Window)
            return current; // not enough signal yet — keep practising

        var window = recentAttempts.Take(Window).ToList();

        // A "confident" correct answer used at most one hint.
        int confident = window.Count(a => a.IsCorrect && a.HintsUsed <= 1);
        int wrong = window.Count(a => !a.IsCorrect);

        if (confident >= Threshold && current < DifficultyLevel.Challenge)
            return current + 1;

        if (wrong >= Threshold && current > DifficultyLevel.Starter)
            return current - 1;

        return current;
    }
}
