namespace LearnEasy.Core.Models;

/// <summary>
/// One child using the app. Progress, score and the current adaptive band
/// all hang off this aggregate root.
/// </summary>
public class LearnerProfile
{
    public int Id { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>Key into the avatar image resources (e.g. "fox", "robot").</summary>
    public string AvatarKey { get; set; } = "fox";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // --- Gamification state -------------------------------------------------
    public int TotalPoints { get; set; }

    /// <summary>Cosmetic level derived from points; see GamificationEngine.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Consecutive correct answers in the current session run.</summary>
    public int CurrentStreak { get; set; }

    public int BestStreak { get; set; }

    // --- Adaptive state -----------------------------------------------------
    /// <summary>The band the next word will be drawn from.</summary>
    public DifficultyLevel CurrentDifficulty { get; set; } = DifficultyLevel.Starter;

    public ICollection<SpellingAttempt> Attempts { get; set; } = new List<SpellingAttempt>();
    public ICollection<LearnerBadge> Badges { get; set; } = new List<LearnerBadge>();
}
