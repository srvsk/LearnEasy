using LearnEasy.Core.Models;

namespace LearnEasy.Core.Abstractions;

/// <summary>
/// Points, levels and badge rules. Mutates the passed profile in memory; the
/// lesson service is responsible for persisting it afterwards.
/// </summary>
public interface IGamificationEngine
{
    /// <summary>Points for one correct answer given hints used and streak.</summary>
    int ScoreAttempt(Word word, int hintsUsed, int currentStreak);

    /// <summary>Recompute <see cref="LearnerProfile.Level"/> from total points.</summary>
    int ComputeLevel(int totalPoints);

    /// <summary>
    /// Evaluate badge rules against the profile + latest attempt and return the
    /// codes of any newly-earned badges (the caller persists the awards).
    /// The <paramref name="word"/> is passed explicitly so the engine never
    /// depends on the attempt's EF navigation being loaded/attached.
    /// </summary>
    IReadOnlyList<string> EvaluateBadges(LearnerProfile profile, SpellingAttempt latest, Word word, IReadOnlyCollection<string> alreadyEarnedCodes);
}
