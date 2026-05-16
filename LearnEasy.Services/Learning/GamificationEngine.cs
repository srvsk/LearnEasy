using LearnEasy.Core.Abstractions;
using LearnEasy.Core.Models;

namespace LearnEasy.Services.Learning;

/// <summary>
/// Points, levels and badge rules. Pure logic — the lesson service applies the
/// returned values to the profile and persists. Tuned to feel generous so kids
/// stay motivated.
/// </summary>
public sealed class GamificationEngine : IGamificationEngine
{
    // Points scale with difficulty so harder words feel more rewarding.
    private static readonly Dictionary<DifficultyLevel, int> BasePoints = new()
    {
        [DifficultyLevel.Starter] = 5,
        [DifficultyLevel.Easy] = 8,
        [DifficultyLevel.Medium] = 12,
        [DifficultyLevel.Hard] = 18,
        [DifficultyLevel.Challenge] = 25,
    };

    public int ScoreAttempt(Word word, int hintsUsed, int currentStreak)
    {
        int points = BasePoints.GetValueOrDefault(word.Difficulty, 5);

        // Each hint halves the reward (min 1) — independence is encouraged.
        for (int i = 0; i < hintsUsed; i++)
            points = Math.Max(1, points / 2);

        // Streak bonus: +2 per consecutive correct answer, capped at +20.
        points += Math.Min(20, currentStreak * 2);

        return points;
    }

    /// <summary>Level grows on a gentle curve: level N needs 50·N·(N-1) points.</summary>
    public int ComputeLevel(int totalPoints)
    {
        int level = 1;
        while (50 * (level + 1) * level <= totalPoints)
            level++;
        return level;
    }

    public IReadOnlyList<string> EvaluateBadges(
        LearnerProfile profile, SpellingAttempt latest, Word word, IReadOnlyCollection<string> alreadyEarnedCodes)
    {
        var earned = new List<string>();

        void TryAward(string code, bool condition)
        {
            if (condition && !alreadyEarnedCodes.Contains(code) && !earned.Contains(code))
                earned.Add(code);
        }

        bool hard = word.Difficulty >= DifficultyLevel.Hard;
        bool challenge = word.Difficulty == DifficultyLevel.Challenge;

        TryAward("first-word", latest.IsCorrect);
        TryAward("streak-5", profile.CurrentStreak >= 5);
        TryAward("streak-10", profile.CurrentStreak >= 10);
        TryAward("no-hints", latest.IsCorrect && latest.HintsUsed == 0 && hard);
        TryAward("challenge-master", latest.IsCorrect && challenge);

        return earned;
    }
}
